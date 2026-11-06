using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using AsuMech.Learn.Sources;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Sources;

public class PptxDeckTests
{
    private static byte[] Deck()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            void Add(string path, string content)
            {
                using var w = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                w.Write(content);
            }
            const string ns = "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"";
            Add("ppt/slides/slide1.xml", $"<p:sld {ns}><p:cSld><p:spTree><p:sp><p:nvSpPr><p:nvPr><p:ph type=\"title\"/></p:nvPr></p:nvSpPr><p:txBody><a:p><a:r><a:t>P2 Diamond </a:t></a:r><a:r><a:t>Dust</a:t></a:r></a:p></p:txBody></p:sp><p:sp><p:txBody><a:p><a:r><a:t>Tanks north</a:t></a:r></a:p><a:p><a:r><a:t>Healers south</a:t></a:r></a:p></p:txBody></p:sp><p:pic><p:blipFill><a:blip r:embed=\"rId2\"/></p:blipFill></p:pic></p:spTree></p:cSld></p:sld>");
            Add("ppt/slides/_rels/slide1.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/notesSlide\" Target=\"../notesSlides/notesSlide1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/image1.png\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://raidplan.io/plan/abc\" TargetMode=\"External\"/></Relationships>");
            Add("ppt/notesSlides/notesSlide1.xml", $"<p:notes {ns}><p:cSld><p:spTree><p:sp><p:txBody><a:p><a:r><a:t>Scene: https://xivplan.netlify.app/#/plan/eJzNkk.</a:t></a:r></a:p></p:txBody></p:sp></p:spTree></p:cSld></p:notes>");
            Add("ppt/media/image1.png", "PNG!");
            Add("ppt/slides/slide2.xml", $"<p:sld {ns}><p:cSld><p:spTree><p:sp><p:txBody><a:p><a:r><a:t>Second</a:t></a:r></a:p></p:txBody></p:sp></p:spTree></p:cSld></p:sld>");
        }
        return ms.ToArray();
    }

    [Test]
    public void SlidesComeOutWithTextNotesImagesAndLinks()
    {
        var slides = PptxDeck.Read(Deck());
        Assert.That(slides.Select(s => s.Number), Is.EqualTo(new[] { 1, 2 }));
        var first = slides[0];
        Assert.That(first.Title, Is.EqualTo("P2 Diamond Dust"));
        Assert.That(first.Texts, Is.EqualTo(new[] { "P2 Diamond Dust", "Tanks north", "Healers south" }));
        Assert.That(first.Notes, Does.StartWith("Scene: https://xivplan"));
        Assert.That(first.Images.Single().Name, Is.EqualTo("image1.png"));
        Assert.That(Encoding.UTF8.GetString(first.Images.Single().Bytes), Is.EqualTo("PNG!"));
        Assert.That(first.Links, Is.EqualTo(new[] { "https://xivplan.netlify.app/#/plan/eJzNkk", "https://raidplan.io/plan/abc" }));
        Assert.That(slides[1].Title, Is.EqualTo("Second"));
        Assert.That(slides[1].Links, Is.Empty);
        Assert.That(PptxDeck.ExportUrl("https://docs.google.com/presentation/d/1AbC_d-e/edit#slide=id.p"), Is.EqualTo("https://docs.google.com/presentation/d/1AbC_d-e/export/pptx"));
    }
}
