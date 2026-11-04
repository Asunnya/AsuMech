using System.Text.Json.Nodes;
using AnoMech.Learn.Sources;
using NUnit.Framework;

namespace AnoMech.Tests.Learn.Sources;

public class TsLiteralTests
{
    [Test]
    public void LiteralsSpreadsReferencesAndCommentsReadAsJson()
    {
        const string ts = """
            import type { Strat } from '$lib/types';
            import { other } from './other';
            // the first block
            const a: Thing[] = [{ mechanic: 'One', n: 1.5, ok: true, nothing: null, 'quoted key': "it's", tpl: `back
            tick` }];
            const b: Thing[] = [{ mechanic: 'Two', neg: -3 }, ...a];
            export const all: Strat = { name: 'x', strats: [...b], cfg: { ...{ p: 1 }, q: 2 }, ref: missing, ext: other, call: rotate(45, 'deg'), escaped: 'a\'b\né' } as const;
            function helper(x: number): string { return `${x}`; }
            """;
        var module = TsLiteral.Parse(ts);
        Assert.That(module.Imports, Is.EqualTo(new[] { "other" }));
        Assert.That(module.Exports, Is.EqualTo(new[] { "all" }));
        var all = module["all"]!.AsObject();
        Assert.That(all["strats"]!.AsArray().Count, Is.EqualTo(2));
        Assert.That(all["strats"]![1]!["mechanic"]!.GetValue<string>(), Is.EqualTo("One"));
        Assert.That(all["strats"]![1]!["tpl"]!.GetValue<string>(), Is.EqualTo("back\ntick"));
        Assert.That(all["strats"]![1]!["quoted key"]!.GetValue<string>(), Is.EqualTo("it's"));
        Assert.That(all["strats"]![0]!["neg"]!.GetValue<long>(), Is.EqualTo(-3));
        Assert.That(all["cfg"]!["p"]!.GetValue<long>(), Is.EqualTo(1));
        Assert.That(all["ref"]!["$ref"]!.GetValue<string>(), Is.EqualTo("missing"));
        Assert.That(all["ext"]!["$ref"]!.GetValue<string>(), Is.EqualTo("other"));
        Assert.That(all["call"]!["$call"]!.GetValue<string>(), Is.EqualTo("rotate"));
        Assert.That(all["call"]!["args"]!.AsArray().Count, Is.EqualTo(2));
        Assert.That(all["escaped"]!.GetValue<string>(), Is.EqualTo("a'b\né"));
        Assert.That(module.Symbols.ContainsKey("helper"), Is.False);
    }

    [Test]
    public void ExternalSymbolsResolveAcrossModules()
    {
        var mits = TsLiteral.Parse("export const planA = { planName: 'a' };");
        var data = TsLiteral.Parse("import { planA } from './mits'; export const cfg = { mitPlans: [planA] };", mits.Symbols);
        Assert.That(data["cfg"]!["mitPlans"]![0]!["planName"]!.GetValue<string>(), Is.EqualTo("a"));
    }
}
