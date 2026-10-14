using Dalamud.Utility.Signatures;

namespace AnoMech.Core.Native.Implementations.Pointers;

// The client's receive handlers for the two duty-only string tables: RSV (localized text the
// sheets ship as "_rsv_..." placeholders) and RSF (obfuscated model / VFX / sound file paths;
// the server sends only the first 64 bytes of each locked file's zlib stream and RsfReceive
// stitches it onto the client-held tail, see xiv.dev/game-internals/rsf). Each takes one raw
// packet payload. FFXIVClientStructs binds neither, so we sig-scan.
//
// The signatures are short and therefore build-sensitive: the properties are nullable so a
// post-patch miss degrades gracefully instead of throwing at load.
internal unsafe class ResolvedStringPointers
{
    // data -> [u32 valueLength][0x30-byte key][value]; tail-calls LayoutWorld::AddRsvString,
    // so Dalamud's RsvResolver hook sees it too.
    [Signature("44 8B 09 4C 8D 41 34", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    public static ReceiveDelegate? RsvReceive { get; private set; }

    // data -> the fixed 0x48-byte record.
    [Signature("48 8B 11 4C 8D 41 08", UseFlags = SignatureUseFlags.Pointer, ScanType = ScanType.Text)]
    public static ReceiveDelegate? RsfReceive { get; private set; }

    public delegate byte ReceiveDelegate(byte* data);

    public static void Initialize()
    {
        Plugin.GameInterop.InitializeFromAttributes(new ResolvedStringPointers());
    }
}
