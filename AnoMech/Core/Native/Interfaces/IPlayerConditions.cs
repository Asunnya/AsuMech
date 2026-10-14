namespace AnoMech.Core.Native.Interfaces;

// The server's PlayerStateFlags packet: the Conditions only the server sets on the local player.
// A status's Lock* flags don't stop the local player by themselves; the client blocks movement
// and actions on these conditions.
public interface IPlayerConditions
{
    // For a status that locks movement or actions.
    bool StatusAffliction { get; }

    // For a status that locks control (Confused): the player's own input is ignored.
    bool ControlAffliction { get; }

    void SetAfflictions(bool status, bool control);

    // The server's close of a client load: BetweenAreas off, and the OccupiedInQuestEvent the
    // client raises at the end of a load while it waits for this.
    void EndClientLoad();
}
