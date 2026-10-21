using System.Linq;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Logging/Memory")]
    public sealed class MemoryLoggerAsset : ScenarioLoggerAsset
    {
        public override IScenarioLogger CreateLogger(ExecutionPlan plan, string runId)
        {
            return new InMemoryScenarioLogger(plan.ScenarioId, runId, plan.Steps.Select(step => step.Id));
        }
    }
}
