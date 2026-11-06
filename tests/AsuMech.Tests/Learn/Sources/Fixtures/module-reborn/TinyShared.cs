namespace BossMod.Dawntrail.Savage.Tiny;

sealed class Cometite : Components.SimpleAOEs
{
    public Cometite(BossModule module) : base(module, (uint)AID.Cometite, 6f, 24)
    {
        MaxDangerColor = 8;
    }
}
sealed class FlatlinerKB(BossModule module) : Components.SimpleKnockbacks(module, (uint)AID.Flatliner1, 15f, true, 1, new AOEShapeCircle(60f));
sealed class HeavyHitter(BossModule module) : Components.GenericBaitAway(module)
{
    private readonly AOEShapeCone healerCone = new(60f, 15.Degrees());
    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.RawSteel_HeavyHitter)
            CurrentBaits.Clear();
    }
}
