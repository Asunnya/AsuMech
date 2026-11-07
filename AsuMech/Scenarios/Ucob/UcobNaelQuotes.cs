namespace AsuMech.Scenarios.Ucob;

public enum NaelMove { LunarDynamo, IronChariot, ThermionicBeam, RavenDive }

public sealed record NaelQuote(string Text, NaelMove First, NaelMove Second);
