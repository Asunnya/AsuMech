using AsuMech.Core.UserActions;
using NUnit.Framework;

namespace AsuMech.Tests.Core.UserActions;

// The Action sheet chains Hunter's/Swiftskin's Sting and Bite from Steel Fangs and Maw only; Reaving
// Fangs and Maw open the same combos in the game, so they must count as those starters.
public class PlayerComboTests
{
    [TestCase(34607u, 34606u, TestName = "Reaving Fangs combos as Steel Fangs")]
    [TestCase(34615u, 34614u, TestName = "Reaving Maw combos as Steel Maw")]
    [TestCase(7526u, 7525u, TestName = "Verholy combos as Verflare")]
    public void AnActionTheSheetDoesNotChainCombosAsItsTwin(uint actionId, uint comboForm)
        => Assert.That(PlayerCombo.ComboId(actionId), Is.EqualTo(comboForm));

    [TestCase(34606u, TestName = "Steel Fangs is its own combo form")]
    [TestCase(34608u, TestName = "Hunter's Sting is its own combo form")]
    public void AnActionTheSheetChainsIsItsOwnComboForm(uint actionId)
        => Assert.That(PlayerCombo.ComboId(actionId), Is.EqualTo(actionId));
}
