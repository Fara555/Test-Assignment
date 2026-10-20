namespace ScenarioConstructor.Conditions
{
    public interface IStepRule
    {
        bool Accepts(string actionId);
        IStepCondition CreateCondition();
    }

    public interface IStepCondition
    {
        ConditionResult Evaluate(StepSignal signal);
    }
}
