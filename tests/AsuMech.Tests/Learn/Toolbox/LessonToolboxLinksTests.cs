using System.Collections.Generic;
using System.Linq;
using AsuMech.Learn;
using AsuMech.Learn.Toolbox;
using NUnit.Framework;

namespace AsuMech.Tests.Learn.Toolbox;

// A lesson's toolbox block points at pages that exist, from steps that exist. The oracle drops a
// dangling link silently, so this is where one shows up.
public class LessonToolboxLinksTests
{
    [Test]
    public void EveryLinkedPageExistsAndEveryStepKeyNamesALessonStep()
    {
        var problems = new List<string>();
        foreach (var lesson in LessonLoader.All)
        {
            if (lesson.Toolbox is not { } link) continue;
            var at = $"{lesson.Scenario}/{lesson.Strat}";
            var plan = ToolboxPlans.Load(link.PlanId);
            if (plan is null) { problems.Add($"{at}: plan {link.PlanId} is not embedded"); continue; }
            var ids = lesson.Steps.Select(s => MasteryStore.StepId(s.Key)).ToHashSet();
            foreach (var (key, page) in link.Steps)
            {
                var id = key.Split('|')[0];
                if (!ids.Contains(id)) problems.Add($"{at}: step {key} is not in the lesson");
                if (page < 0 || page >= plan.Steps.Count) problems.Add($"{at}: step {key} → page {page}, plan has {plan.Steps.Count}");
            }
            foreach (var page in link.Pages ?? [])
                if (page < 0 || page >= plan.Steps.Count) problems.Add($"{at}: extra page {page}, plan has {plan.Steps.Count}");
            var slots = plan.Steps.SelectMany(s => s.Objects).Where(o => o.Kind == "player").Select(o => o.Key).ToHashSet();
            foreach (var (role, slot) in link.RoleMap)
                if (slots.Count > 0 && !slots.Contains(slot)) problems.Add($"{at}: {role} → {slot}, the plan has {string.Join(",", slots.Order())}");
        }
        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }
}
