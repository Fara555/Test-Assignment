using System.Linq;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Logging/Memory With Console Output")]
    public sealed class ConsoleLoggerAsset : ScenarioLoggerAsset
    {
        public override IScenarioLogger CreateLogger(ExecutionPlan plan, string runId)
        {
            return new ConsoleLogger(new InMemoryScenarioLogger(plan.ScenarioId, runId,
                plan.Steps.Select(step => step.Id)));
        }

        private sealed class ConsoleLogger : IScenarioLogger
        {
            private readonly IScenarioLogger storage;

            public ConsoleLogger(IScenarioLogger storage) => this.storage = storage;

            public void Record(ScenarioEvent entry)
            {
                storage.Record(entry);
                Debug.Log($"[Scenario] {entry.StepId}: {entry.EventType}, {entry.Outcome}, attempt {entry.AttemptNumber}");
            }

            public ScenarioJournal Capture() => storage.Capture();
        }
    }
}
