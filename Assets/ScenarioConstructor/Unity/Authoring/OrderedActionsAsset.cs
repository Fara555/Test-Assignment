using System.Collections.Generic;
using ScenarioConstructor.Conditions;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Conditions/Ordered Actions")]
    public sealed class OrderedActionsAsset : StepRuleAsset
    {
        [SerializeField, Tooltip("Actions must arrive in this order. Use one entry for a simple step.")]
        private List<string> actions = new List<string> { "perform" };

        public override IStepRule CreateRule() => new OrderedActionsRule(actions.ToArray());
    }
}
