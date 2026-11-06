namespace BossMod.Dawntrail.Savage.Tiny;

class TinyStates : StateMachineBuilder
{
    public TinyStates(BossModule module) : base(module)
    {
        DeathPhase(0, SinglePhase);
    }

    private void SinglePhase(uint id)
    {
        TrophyWeapons(id, 5.2f);
        Cast(id + 0x70000, AID.CrownOfArcadia, 5, 8, "Boss disappears (enrage)");
    }

    void TrophyWeapons(uint id, float delay)
    {
        Cast(id, AID.CrownOfArcadia, delay, 5, "Raidwide")
            .SetHint(StateMachine.StateHint.Raidwide)
            .ActivateOnEnter<CrownOfArcadia>()
            .DeactivateOnExit<CrownOfArcadia>();

        Cast(id + 0x100, AID.RawSteelTrophyAxe, 10.5f, 3)
            .ActivateOnEnter<RawSteel>();
        ComponentCondition<RawSteel>(id + 0x112, 2.2f, t => t.NumCasts > 0, "Weapon 1");
        ComponentCondition<RawSteel>(id + 0x113, 5.2f, t => t.NumCasts > 1, "Weapon 2")
            .DeactivateOnExit<RawSteel>();
    }
}
