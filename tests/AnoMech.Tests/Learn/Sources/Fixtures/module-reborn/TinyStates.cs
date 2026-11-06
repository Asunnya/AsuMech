namespace BossMod.Dawntrail.Savage.Tiny;

sealed class TinyStates : StateMachineBuilder
{
    public TinyStates(BossModule module) : base(module)
    {
        DeathPhase(default, SinglePhase);
    }

    private void SinglePhase(uint id)
    {
        Cast(id, AID.CrownOfArcadia, 0f, 5f, "Crown of Arcadia")
            .SetHint(StateMachine.StateHint.Raidwide)
            .ActivateOnEnter<Cometite>();
        Condition(id + 0x10u, 362f, () => Module.PrimaryActor.CastInfo?.Action.ID == (uint)AID.Flatliner1, "Majestic Meteor", 50, 0);
        SimpleState(id + 0x30u, 1000f, "Final Phase");
    }
}
