using System.Collections.Generic;
using System.Text;
using AsuMech.Windows.Asu;
using NUnit.Framework;

namespace AsuMech.Tests;

public class SupportWindowTests
{
    [Test]
    public void ThePixQrCodeIsEmbedded()
        => Assert.That(typeof(SupportWindow).Assembly.GetManifestResourceInfo("AsuMech.Windows.Asu.Images.pix-qr.png"), Is.Not.Null);

    // Bank apps refuse a code whose last four characters are not the CRC16-CCITT of the rest.
    [Test]
    public void ThePixCodeHasAValidChecksum()
    {
        var code = SupportWindow.PixCode;
        Assert.That(code[^8..^4], Is.EqualTo("6304"));
        Assert.That(Crc16(code[..^4]), Is.EqualTo(code[^4..]));
    }

    // The code is public: it carries the key and a placeholder name, nothing about the holder.
    [Test]
    public void ThePixCodeCarriesOnlyTheKeyAndAPlaceholderName()
    {
        var fields = Fields(SupportWindow.PixCode[..^8]);
        Assert.That(Fields(fields["26"])["01"], Is.EqualTo(SupportWindow.PixKey));
        Assert.That(fields["59"], Is.EqualTo("ASUMECH"));
        Assert.That(Fields(fields["62"])["05"], Is.EqualTo("***"));
    }

    private static Dictionary<string, string> Fields(string tlv)
    {
        var fields = new Dictionary<string, string>();
        for (var i = 0; i < tlv.Length;)
        {
            var length = int.Parse(tlv.Substring(i + 2, 2));
            fields[tlv.Substring(i, 2)] = tlv.Substring(i + 4, length);
            i += 4 + length;
        }
        return fields;
    }

    private static string Crc16(string payload)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.UTF8.GetBytes(payload))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc.ToString("X4");
    }
}
