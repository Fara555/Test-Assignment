using System;
using ScenarioConstructor.Conditions;

namespace ScenarioConstructor.Execution
{
    public sealed class StepDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public IStepRule Rule { get; }

        public StepDefinition(string id, string title, IStepRule rule)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A step identifier is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("A step title is required.", nameof(title));
            Id = id;
            Title = title;
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
    }
}
