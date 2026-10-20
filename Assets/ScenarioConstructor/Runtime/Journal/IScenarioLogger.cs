namespace ScenarioConstructor.Journal
{
    public interface IScenarioLogger
    {
        void Record(ScenarioEvent entry);
        ScenarioJournal Capture();
    }
}
