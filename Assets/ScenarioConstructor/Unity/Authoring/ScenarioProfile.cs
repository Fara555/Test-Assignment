using System;
using System.Collections.Generic;
using ScenarioConstructor.Execution;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Profile")]
    public sealed class ScenarioProfile : ScriptableObject
    {
        [SerializeField] private string scenarioId = "scenario";
        [SerializeField, Tooltip("Shared step assets. List order does not control activation.")]
        private List<ScenarioStepAsset> steps = new List<ScenarioStepAsset>();
        [SerializeField, Tooltip("Sequential groups with independent violation budgets.")]
        private List<StepSetConfiguration> groups = new List<StepSetConfiguration>();
        [SerializeField, Tooltip("Reorder these batches to change activation. Members of one batch open together.")]
        private List<StepSetConfiguration> activationBatches = new List<StepSetConfiguration>();
        [SerializeField] private PendingActionBehavior pendingActions;
        [SerializeField] private ExecutionPolicyAsset execution;
        [SerializeField] private ScenarioLoggerAsset logger;
        [SerializeField] private ScenarioAnalyzerAsset analyzer;

        public IReadOnlyList<ScenarioStepAsset> Steps => steps.AsReadOnly();
        public ScenarioAnalyzerAsset Analyzer => analyzer;

        public ExecutionPlan BuildPlan()
        {
            if (execution == null || logger == null || analyzer == null)
                throw new InvalidOperationException("Select execution, logger, and analyzer assets.");

            var definitions = new List<StepDefinition>();
            var roster = new HashSet<ScenarioStepAsset>();
            foreach (var step in steps)
            {
                if (step == null || !roster.Add(step))
                    throw new InvalidOperationException("The step roster must contain nonnull, unique asset references.");
                definitions.Add(step.BuildDefinition());
            }

            var plan = new ExecutionPlan(scenarioId, definitions, BuildSets(groups, roster),
                BuildSets(activationBatches, roster), pendingActions);
            var policy = execution.CreatePolicy()
                ?? throw new InvalidOperationException("The execution asset did not create a policy.");
            policy.Validate(plan);
            return plan;
        }

        public ScenarioRunner CreateRunner()
        {
            var plan = BuildPlan();
            var selectedLogger = logger;
            return new ScenarioRunner(plan, execution.CreatePolicy(), id => selectedLogger.CreateLogger(plan, id));
        }

        public bool TryValidate(out string diagnostic)
        {
            try
            {
                BuildPlan();
                diagnostic = "Configuration is valid.";
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                diagnostic = exception.Message;
                return false;
            }
        }

        private static List<StepSet> BuildSets(List<StepSetConfiguration> settings, ISet<ScenarioStepAsset> roster)
        {
            var result = new List<StepSet>();
            foreach (var set in settings)
            {
                if (set == null)
                    throw new InvalidOperationException("Group and activation entries cannot be null.");
                result.Add(set.Build(roster));
            }
            return result;
        }
    }
}
