namespace AnoMech.Core.Native.Interfaces;

// Excel sheets and game files. Rows are our own records, not Lumina's: tests have no game files.
public interface IGameData
{
    ActionRow? Action(uint actionId);
    KnockbackRow? Knockback(uint knockbackId);
    ClassJobRow? ClassJob(uint classJobId);
    BNpcBaseRow? BNpcBase(uint bnpcBaseId);
    ModelCharaRow? ModelChara(uint modelCharaId);
    ModelSkeletonRow? ModelSkeleton(uint skeletonId);
    StatusRow? Status(ushort statusId);

    // Null for a missing row or an empty name.
    string? BNpcName(uint nameId);

    bool FileExists(string path);
}

public static class GameDataExtensions
{
    // Falls back to the raw id so a message naming the row never goes blank.
    public static string ActionName(this IGameData data, uint actionId)
        => data.Action(actionId) is { Name.Length: > 0 } row ? row.Name : actionId.ToString();

    public static string StatusName(this IGameData data, ushort statusId)
        => data.Status(statusId) is { Name.Length: > 0 } row ? row.Name : statusId.ToString();
}
