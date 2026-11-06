namespace AnoMech.Core.Native.Interfaces;

// HighEndDuty marks the current tier only: an older savage reads false, the current extreme true.
public sealed record ContentFinderConditionRow(uint Id, byte ClassJobLevelSync, string Name = "", byte ContentType = 0, bool HighEndDuty = false)
{
    public const byte TrialContentType = 4;
    public const byte UltimateContentType = 28;
}
