using System;
using ScenarioConstructor.Execution;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Step")]
    public sealed class ScenarioStepAsset : ScriptableObject
    {
        [SerializeField, Tooltip("Stable identifier, unique within a scenario.")]
        private string stepId;
        [SerializeField] private string title;
        [SerializeField] private StepRuleAsset rule;

        public string StepId => stepId;
        public string Title => title;

        public StepDefinition BuildDefinition()
        {
            if (rule == null)
                throw new InvalidOperationException($"Step '{name}' needs a condition asset.");
            return new StepDefinition(stepId, title, rule.CreateRule());
        }
    }
}
