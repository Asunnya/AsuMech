using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AnoMech.Learn.Sources;

// One slide of an exported deck: its text runs, its speaker notes, the images it embeds and the
// links found in either text (a strat deck usually pastes the XIVPlan link in the notes).
public sealed record PptxSlide(int Number, string Title, List<string> Texts, string Notes, List<(string Name, byte[] Bytes)> Images, List<string> Links);

// A .pptx (a Google Slides deck exported without login at /export/pptx) read as a zip of XML: text
// from a:t runs, notes from the notes slide, pictures from the slide's relationships.
public static class PptxDeck
{
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly Regex Link = new(@"https?://[^\s""'<>)\]]+", RegexOptions.Compiled);

    public static string ExportUrl(string deckIdOrUrl)
    {
        var id = deckIdOrUrl.Trim();
        var m = Regex.Match(id, @"/presentation/d/([A-Za-z0-9_-]+)");
        if (m.Success) id = m.Groups[1].Value;
        return $"https://docs.google.com/presentation/d/{id}/export/pptx";
    }

    public static List<PptxSlide> Read(byte[] pptx)
    {
        using var zip = new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read);
        var slides = new List<PptxSlide>();
        foreach (var entry in zip.Entries.Where(e => Regex.IsMatch(e.FullName, @"^ppt/slides/slide\d+\.xml$")).OrderBy(e => int.Parse(Regex.Match(e.Name, @"\d+").Value)))
        {
            var number = int.Parse(Regex.Match(entry.Name, @"\d+").Value);
            var xml = Load(entry);
            var texts = Paragraphs(xml);
            var title = xml.Descendants(P + "sp").FirstOrDefault(sp => sp.Descendants(P + "ph").Any(ph => (string?)ph.Attribute("type") is "title" or "ctrTitle"))
                ?.Descendants(A + "t").Select(t => t.Value).Aggregate("", (acc, t) => acc + t).Trim() ?? texts.FirstOrDefault() ?? "";
            var rels = zip.GetEntry($"ppt/slides/_rels/{entry.Name}.rels") is { } relEntry ? Load(relEntry) : null;
            var images = new List<(string, byte[])>();
            var notes = "";
            if (rels != null)
                foreach (var rel in rels.Descendants(Rel + "Relationship"))
                {
                    var type = (string?)rel.Attribute("Type") ?? "";
                    var target = (string?)rel.Attribute("Target") ?? "";
                    var path = Resolve("ppt/slides/", target);
                    if (type.EndsWith("/image", StringComparison.Ordinal) && zip.GetEntry(path) is { } image) images.Add((Path.GetFileName(path), Bytes(image)));
                    else if (type.EndsWith("/notesSlide", StringComparison.Ordinal) && zip.GetEntry(path) is { } note) notes = string.Join("\n", Paragraphs(Load(note)));
                }
            var links = Link.Matches(string.Join("\n", texts) + "\n" + notes).Select(m => m.Value.TrimEnd('.', ',')).Distinct().ToList();
            if (rels != null)
                links.AddRange(rels.Descendants(Rel + "Relationship").Where(r => ((string?)r.Attribute("TargetMode")) == "External").Select(r => (string?)r.Attribute("Target") ?? "").Where(t => t.StartsWith("http", StringComparison.Ordinal) && !links.Contains(t)));
            slides.Add(new PptxSlide(number, title, texts, notes, images, links));
        }
        return slides;
    }

    private static List<string> Paragraphs(XDocument xml)
        => xml.Descendants(A + "p").Select(p => string.Concat(p.Descendants(A + "t").Select(t => t.Value)).Trim()).Where(t => t.Length > 0).ToList();

    private static XDocument Load(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static byte[] Bytes(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static string Resolve(string baseDir, string target)
    {
        var parts = new List<string>(baseDir.TrimEnd('/').Split('/'));
        foreach (var part in target.Split('/'))
        {
            if (part == "..") { if (parts.Count > 0) parts.RemoveAt(parts.Count - 1); }
            else if (part != ".") parts.Add(part);
        }
        return string.Join("/", parts);
    }
}
