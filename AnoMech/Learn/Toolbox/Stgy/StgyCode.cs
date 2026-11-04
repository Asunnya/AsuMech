using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnoMech.Learn.Toolbox.Stgy;

// One object on a Strategy Board: board pixels (origin top-left, y down, 512x384), degrees
// clockwise, size in percent, colour with transparency in percent, and the type's params.
public sealed record StgyObject(int Index, int TypeId, float X, float Y, int Angle, int Size, (byte R, byte G, byte B, byte Transparency) Color,
    int Flags, int A, int B, int C, string? Text)
{
    public string TypeName => StgyTypes.Name(TypeId);
    public bool Visible => (Flags & 1) != 0;
    public bool Locked => (Flags & 8) != 0;
}

public sealed record StgyDocument(uint Version, string Name, int Background, IReadOnlyList<StgyObject> Objects)
{
    public string BackgroundName => Background switch
    {
        1 => "none", 2 => "checkered", 3 => "checkered_circle", 4 => "checkered_square", 5 => "grey", 6 => "grey_circle", 7 => "grey_square", _ => $"background_{Background}",
    };
}

// The game's Strategy Board share code: "[stgy:a" + key char + ciphered base64url + "]". The
// cipher is a fixed substitution plus a shift by position and key; the bytes are a 6-byte prefix
// (crc32, decompressed length) and a zlib stream holding a little-endian document of typed objects
// with parallel vectors (flags, position, angle, size, colour, params) and the texts.
public static class StgyCode
{
    public const float BoardWidth = 512f;
    public const float BoardHeight = 384f;

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-_";

    // Payload char -> substitute (the same bijection the key char goes through).
    private static readonly Dictionary<char, char> Subst = new()
    {
        ['b'] = '-', ['2'] = '0', ['w'] = '1', ['7'] = '2', ['q'] = '3', ['S'] = '4', ['t'] = '5', ['E'] = '6', ['V'] = '7', ['4'] = '8', ['P'] = '9',
        ['f'] = 'A', ['R'] = 'B', ['e'] = 'C', ['A'] = 'D', ['F'] = 'E', ['B'] = 'F', ['u'] = 'G', ['d'] = 'H', ['k'] = 'I', ['6'] = 'J', ['3'] = 'K',
        ['K'] = 'L', ['L'] = 'M', ['+'] = 'N', ['Y'] = 'O', ['-'] = 'P', ['z'] = 'Q', ['T'] = 'R', ['5'] = 'S', ['D'] = 'T', ['n'] = 'U', ['H'] = 'V',
        ['h'] = 'W', ['Q'] = 'X', ['U'] = 'Y', ['9'] = 'Z', ['W'] = '_', ['G'] = 'a', ['Z'] = 'b', ['I'] = 'c', ['j'] = 'd', ['N'] = 'e', ['r'] = 'f',
        ['1'] = 'g', ['m'] = 'h', ['a'] = 'i', ['O'] = 'j', ['p'] = 'k', ['o'] = 'l', ['M'] = 'm', ['X'] = 'n', ['i'] = 'o', ['J'] = 'p',
        ['l'] = 'q', ['g'] = 'r', ['8'] = 's', ['C'] = 't', ['x'] = 'u', ['c'] = 'v', ['v'] = 'w', ['0'] = 'x', ['s'] = 'y', ['y'] = 'z',
    };

    public static readonly Regex Pattern = new(@"\[stgy:a[A-Za-z0-9+\-_]{2,}\]", RegexOptions.Compiled);

    public static bool Looks(string text) => Pattern.IsMatch(text);

    public static StgyDocument Decode(string code)
    {
        code = code.Trim();
        if (!code.StartsWith("[stgy:a", StringComparison.Ordinal) || !code.EndsWith(']')) throw new InvalidDataException("not a [stgy:a…] code");
        var body = code[7..^1];
        if (body.Length < 2) throw new InvalidDataException("code body too short");
        if (body.Any(c => !Alphabet.Contains(c))) throw new InvalidDataException("code has a character outside its alphabet");
        if (!Subst.TryGetValue(body[0], out var keyChar)) throw new InvalidDataException("invalid key character");
        var key = Index(keyChar);
        var plain = new StringBuilder(body.Length - 1);
        for (var i = 0; i < body.Length - 1; i++)
        {
            var sub = Subst.GetValueOrDefault(body[i + 1], 'A');
            plain.Append(Char((Index(sub) - i - key) & 63));
        }
        var b64 = plain.ToString().Replace('-', '+').Replace('_', '/');
        b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(b64);
        if (bytes.Length < 7) throw new InvalidDataException("code holds no document");
        return Parse(Inflate(bytes.AsSpan(6).ToArray()));
    }

    private static byte[] Inflate(byte[] zlib)
    {
        using var input = new MemoryStream(zlib);
        using var stream = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    public static StgyDocument Parse(byte[] doc)
    {
        if (doc.Length < 28) throw new InvalidDataException("document shorter than its header");
        var version = BitConverter.ToUInt32(doc, 0);
        var pos = 24;
        var nameTag = BitConverter.ToUInt16(doc, pos);
        var nameLen = BitConverter.ToUInt16(doc, pos + 2);
        pos += 4;
        var name = nameTag == 1 && pos + nameLen <= doc.Length ? Encoding.UTF8.GetString(doc, pos, nameLen).TrimEnd('\0').Trim() : "";
        pos += nameLen;

        var types = new List<int>();
        var texts = new Queue<string>();
        var flags = new List<int>();
        var xy = new List<(float, float)>();
        var angle = new List<int>();
        var size = new List<int>();
        var color = new List<(byte, byte, byte, byte)>();
        var a = new List<int>();
        var b = new List<int>();
        var c = new List<int>();
        var background = 1;
        var vectorsStarted = false;
        while (pos + 2 <= doc.Length)
        {
            var tag = BitConverter.ToUInt16(doc, pos);
            pos += 2;
            if (tag == 2)
            {
                if (pos + 2 > doc.Length) break;
                if (!vectorsStarted) types.Add(BitConverter.ToUInt16(doc, pos));
                pos += 2;
                continue;
            }
            if (tag == 3)
            {
                if (pos + 2 > doc.Length) break;
                var len = BitConverter.ToUInt16(doc, pos);
                pos += 2;
                if (len == 1)
                {
                    if (pos + 4 <= doc.Length) background = BitConverter.ToUInt16(doc, pos + 2);
                    break;
                }
                if (pos + len > doc.Length) break;
                texts.Enqueue(Encoding.UTF8.GetString(doc, pos, len).TrimEnd('\0'));
                pos += len;
                continue;
            }
            if (tag is 4 or 5 or 6 or 7 or 8 or 10 or 11 or 12)
            {
                vectorsStarted = true;
                if (pos + 4 > doc.Length) break;
                var count = BitConverter.ToUInt16(doc, pos + 2);
                pos += 4;
                var width = tag switch { 5 or 8 => 4, 7 => 1, _ => 2 };
                var bytes = count * width;
                if (tag == 7) bytes += count % 2;
                if (pos + bytes > doc.Length) break;
                for (var i = 0; i < count; i++)
                {
                    var at = pos + i * width;
                    switch (tag)
                    {
                        case 4: flags.Add(BitConverter.ToUInt16(doc, at)); break;
                        case 5: xy.Add((BitConverter.ToInt16(doc, at) / 10f, BitConverter.ToInt16(doc, at + 2) / 10f)); break;
                        case 6: angle.Add(BitConverter.ToInt16(doc, at)); break;
                        case 7: size.Add(doc[at]); break;
                        case 8: color.Add((doc[at], doc[at + 1], doc[at + 2], doc[at + 3])); break;
                        case 10: a.Add(BitConverter.ToUInt16(doc, at)); break;
                        case 11: b.Add(BitConverter.ToUInt16(doc, at)); break;
                        case 12: c.Add(BitConverter.ToUInt16(doc, at)); break;
                    }
                }
                pos += bytes;
                continue;
            }
            break;
        }

        var objects = new List<StgyObject>();
        for (var i = 0; i < types.Count; i++)
        {
            var (x, y) = i < xy.Count ? xy[i] : (0f, 0f);
            var s = i < size.Count && size[i] > 0 ? size[i] : 100;
            var text = types[i] == 100 && texts.Count > 0 ? texts.Dequeue() : null;
            objects.Add(new StgyObject(i, types[i], x, y, i < angle.Count ? angle[i] : 0, s, i < color.Count ? color[i] : ((byte)255, (byte)255, (byte)255, (byte)0),
                i < flags.Count ? flags[i] : 1, i < a.Count ? a[i] : 0, i < b.Count ? b[i] : 0, i < c.Count ? c[i] : 0, text));
        }
        return new StgyDocument(version, name, background, objects);
    }

    private static int Index(char ch) => ch switch
    {
        >= 'A' and <= 'Z' => ch - 'A',
        >= 'a' and <= 'z' => ch - 'a' + 26,
        >= '0' and <= '9' => ch - '0' + 52,
        '-' => 62,
        '_' => 63,
        _ => 0,
    };

    private static char Char(int v)
    {
        v &= 63;
        return v < 26 ? (char)('A' + v) : v < 52 ? (char)('a' + v - 26) : v < 62 ? (char)('0' + v - 52) : v == 62 ? '-' : '_';
    }
}

public static class StgyTypes
{
    private static readonly Dictionary<int, string> Names = new()
    {
        [4] = "checkered_circle", [8] = "checkered_square", [124] = "grey_circle", [125] = "grey_square",
        [9] = "circle_aoe", [10] = "fan_aoe", [11] = "line_aoe", [12] = "line", [13] = "gaze", [14] = "stack", [15] = "line_stack", [16] = "proximity", [17] = "donut",
        [106] = "stack_multi", [107] = "proximity_player", [108] = "tankbuster", [109] = "radial_knockback", [110] = "linear_knockback", [111] = "tower", [112] = "targeting",
        [126] = "moving_circle_aoe", [127] = "1person_aoe", [128] = "2person_aoe", [129] = "3person_aoe", [130] = "4person_aoe",
        [18] = "gladiator", [19] = "pugilist", [20] = "marauder", [21] = "lancer", [22] = "archer", [23] = "conjurer", [24] = "thaumaturge", [25] = "arcanist", [26] = "rogue",
        [27] = "paladin", [28] = "monk", [29] = "warrior", [30] = "dragoon", [31] = "bard", [32] = "white_mage", [33] = "black_mage", [34] = "summoner", [35] = "scholar",
        [36] = "ninja", [37] = "machinist", [38] = "dark_knight", [39] = "astrologian", [40] = "samurai", [41] = "red_mage", [42] = "blue_mage", [43] = "gunbreaker",
        [44] = "dancer", [45] = "reaper", [46] = "sage", [101] = "viper", [102] = "pictomancer",
        [47] = "tank", [48] = "tank_1", [49] = "tank_2", [50] = "healer", [51] = "healer_1", [52] = "healer_2", [53] = "dps", [54] = "dps_1", [55] = "dps_2", [56] = "dps_3", [57] = "dps_4",
        [118] = "melee_dps", [119] = "ranged_dps", [120] = "physical_ranged_dps", [121] = "magical_ranged_dps", [122] = "pure_healer", [123] = "barrier_healer",
        [60] = "small_enemy", [62] = "medium_enemy", [64] = "large_enemy",
        [65] = "attack_1", [66] = "attack_2", [67] = "attack_3", [68] = "attack_4", [69] = "attack_5", [115] = "attack_6", [116] = "attack_7", [117] = "attack_8",
        [70] = "bind_1", [71] = "bind_2", [72] = "bind_3", [73] = "ignore_1", [74] = "ignore_2", [75] = "square_marker", [76] = "circle_marker", [77] = "plus_marker", [78] = "triangle_marker",
        [79] = "waymark_a", [80] = "waymark_b", [81] = "waymark_c", [82] = "waymark_d", [83] = "waymark_1", [84] = "waymark_2", [85] = "waymark_3", [86] = "waymark_4",
        [87] = "shape_circle", [88] = "shape_x", [89] = "shape_triangle", [90] = "shape_square", [94] = "up_arrow", [100] = "text", [103] = "rotate", [105] = "group",
        [113] = "enhancement", [114] = "enfeeblement", [131] = "lockon_red", [132] = "lockon_blue", [133] = "lockon_purple", [134] = "lockon_green",
        [135] = "highlighted_circle", [136] = "highlighted_x", [137] = "highlighted_square", [138] = "highlighted_triangle", [139] = "rotate_clockwise", [140] = "rotate_counterclockwise",
    };

    // ClassJob row id -> abbreviation, for the jobs a board can place.
    public static readonly Dictionary<int, string> Jobs = new()
    {
        [18] = "GLA", [19] = "PGL", [20] = "MRD", [21] = "LNC", [22] = "ARC", [23] = "CNJ", [24] = "THM", [25] = "ACN", [26] = "ROG",
        [27] = "PLD", [28] = "MNK", [29] = "WAR", [30] = "DRG", [31] = "BRD", [32] = "WHM", [33] = "BLM", [34] = "SMN", [35] = "SCH", [36] = "NIN",
        [37] = "MCH", [38] = "DRK", [39] = "AST", [40] = "SAM", [41] = "RDM", [42] = "BLU", [43] = "GNB", [44] = "DNC", [45] = "RPR", [46] = "SGE", [101] = "VPR", [102] = "PCT",
    };

    public static string Name(int id) => Names.TryGetValue(id, out var n) ? n : $"unknown_{id}";
    public static bool IsJob(int id) => Jobs.ContainsKey(id);
    public static bool IsWaymark(int id) => id is >= 79 and <= 86;
    public static bool IsEnemy(int id) => id is 60 or 62 or 64;
    public static bool IsAoe(int id) => id is 4 or 8 or 124 or 125 or (>= 9 and <= 17) or (>= 106 and <= 112) or (>= 126 and <= 130);
}
