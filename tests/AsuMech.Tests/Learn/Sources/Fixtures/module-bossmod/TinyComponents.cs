namespace BossMod.Dawntrail.Savage.Tiny;

class CrownOfArcadia(BossModule module) : Components.RaidwideCast(module, AID.CrownOfArcadia);

class RawSteel(BossModule module) : Components.GenericBaitAway(module)
{
    private static readonly AOEShapeCone _heavyHitter = new(60, 22.5f.Degrees());

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if ((AID)spell.Action.ID == AID.RawSteelScytheHeavyHitter)
            CurrentBaits.Add(new(caster, caster, _heavyHitter));
    }
}

class ScytheAOE(BossModule module) : Components.StandardAOEs(module, AID.AssaultEvolvedScytheAOE, new AOEShapeDonut(5, 60));
class SwordAOE(BossModule module) : Components.StandardAOEs(module, AID.AssaultEvolvedSwordAOE, new AOEShapeCross(40, 5));
class SharpTaste(BossModule module) : Components.StandardAOEs(module, AID.SharpTaste, new AOEShapeRect(60, 3));
class Towers(BossModule module) : Components.GenericTowers(module, AID.Bare); // no radius here: unparsed
