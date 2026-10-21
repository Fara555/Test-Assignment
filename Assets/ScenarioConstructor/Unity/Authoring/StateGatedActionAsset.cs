using ScenarioConstructor.Conditions;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Conditions/State Gated Action")]
    public sealed class StateGatedActionAsset : StepRuleAsset
    {
        [SerializeField] private string actionId = "perform";
        [SerializeField, Tooltip("The action relay must supply this flag from a scene state component.")]
        private string flagId = "powerOn";
        [SerializeField] private bool expectedValue = true;

        public override IStepRule CreateRule() => new StateGatedActionRule(actionId, flagId, expectedValue);
    }
}
