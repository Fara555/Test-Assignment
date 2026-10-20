using System;
using System.Collections.Generic;
using NUnit.Framework;
using ScenarioConstructor.Analysis;
using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Tests
{
    public sealed class JournalAnalysisTests
    {
        private static readonly DateTimeOffset Timestamp =
            new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void TrainingReferenceExampleScoresFifteenOutOfThirty()
        {
            var journal = Journal(Resolved("a", StepOutcome.Completed, 1),
                Resolved("b", StepOutcome.Completed, 2), Resolved("c", StepOutcome.Completed, 3));

            var result = new TrainingAnalyzer().Analyze(journal);

            Assert.That(result.Score, Is.EqualTo(15));
            Assert.That(result.MaximumScore, Is.EqualTo(30));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void ExamReferenceExampleScoresSeventy()
        {
            var journal = Journal(Resolved("a", StepOutcome.Failed), Resolved("b", StepOutcome.Failed),
                Resolved("c", StepOutcome.Skipped), Resolved("d", StepOutcome.Skipped));

            var result = new ExamAnalyzer().Analyze(journal);

            Assert.That(result.Score, Is.EqualTo(70));
            Assert.That(result.MaximumScore, Is.EqualTo(100));
            Assert.That(result.IsComplete, Is.True);
        }

        [Test]
        public void FourthAndLaterAttemptsEarnNoTrainingPoints()
        {
            var result = new TrainingAnalyzer().Analyze(Journal(Resolved("a", StepOutcome.Completed, 4)));
            Assert.That(result.Score, Is.Zero);
        }

        [Test]
        public void AllCompletedExamStepsEarnOneHundred()
        {
            var result = new ExamAnalyzer().Analyze(Journal(Resolved("a", StepOutcome.Completed)));
            Assert.That(result.Score, Is.EqualTo(100));
        }

        [Test]
        public void ExamScoreCannotBeNegative()
        {
            var events = new List<ScenarioEvent>();
            for (var i = 0; i < 11; i++)
                events.Add(Resolved($"step-{i}", StepOutcome.Failed));

            Assert.That(new ExamAnalyzer().Analyze(Journal(events.ToArray())).Score, Is.Zero);
        }

        [Test]
        public void SameSnapshotCanBeAnalyzedRepeatedlyWithDifferentAnalyzers()
        {
            var journal = Journal(Resolved("a", StepOutcome.Completed, 2));

            Assert.That(new TrainingAnalyzer().Analyze(journal).Score, Is.EqualTo(5));
            Assert.That(new ExamAnalyzer().Analyze(journal).Score, Is.EqualTo(100));
            Assert.That(new TrainingAnalyzer().Analyze(journal).Score, Is.EqualTo(5));
            Assert.That(journal.Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void MissingAttemptNumberProducesWarningAndConservativeTrainingScore()
        {
            var result = new TrainingAnalyzer().Analyze(Journal(Resolved("a", StepOutcome.Completed)));
            Assert.That(result.Score, Is.Zero);
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnknownEventTypesAndAdditionalFieldsDoNotChangeScoring()
        {
            var logger = new InMemoryScenarioLogger("scenario", "run", new[] { "a" });
            logger.Record(new ScenarioEvent(Timestamp, "a", "custom.telemetry", StepOutcome.Failed,
                details: new Dictionary<string, string> { { "temperature", "20" } }));
            logger.Record(Resolved("a", StepOutcome.Completed, 1));

            Assert.That(new TrainingAnalyzer().Analyze(logger.Capture()).Score, Is.EqualTo(10));
            Assert.That(new ExamAnalyzer().Analyze(logger.Capture()).Score, Is.EqualTo(100));
        }

        [Test]
        public void RejectedAttemptsDoNotBecomeTerminalFailures()
        {
            var logger = new InMemoryScenarioLogger("scenario", "run", new[] { "a" });
            logger.Record(new ScenarioEvent(Timestamp, "a", ScenarioEvent.AttemptRejected, attemptNumber: 1));
            logger.Record(Resolved("a", StepOutcome.Completed, 2));

            Assert.That(new TrainingAnalyzer().Analyze(logger.Capture()).Score, Is.EqualTo(5));
            Assert.That(new ExamAnalyzer().Analyze(logger.Capture()).Score, Is.EqualTo(100));
        }

        [Test]
        public void CapturedJournalDoesNotChangeWhenLoggerReceivesMoreEvents()
        {
            var logger = new InMemoryScenarioLogger("scenario", "run", new[] { "a" });
            var snapshot = logger.Capture();
            logger.Record(Resolved("a", StepOutcome.Completed, 1));

            Assert.That(snapshot.Events, Is.Empty);
            Assert.That(logger.Capture().Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void EventCopiesAdditionalFields()
        {
            var details = new Dictionary<string, string> { { "reason", "Original" } };
            var entry = new ScenarioEvent(Timestamp, "a", "custom.event", details: details);
            details["reason"] = "Changed";

            Assert.That(entry.Details["reason"], Is.EqualTo("Original"));
        }

        [Test]
        public void JournalCopiesItsInputCollections()
        {
            var ids = new List<string> { "a" };
            var events = new List<ScenarioEvent> { Resolved("a", StepOutcome.Completed, 1) };
            var journal = new ScenarioJournal("scenario", "run", ids, events);
            ids.Clear();
            events.Clear();

            Assert.That(journal.StepIds.Count, Is.EqualTo(1));
            Assert.That(journal.Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnresolvedStepsProduceProvisionalResult()
        {
            var journal = new ScenarioJournal("scenario", "run", new[] { "a", "b" },
                new[] { Resolved("a", StepOutcome.Completed, 1) });
            var result = new TrainingAnalyzer().Analyze(journal);

            Assert.That(result.Score, Is.EqualTo(10));
            Assert.That(result.MaximumScore, Is.EqualTo(20));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Warnings, Is.Not.Empty);
        }

        [Test]
        public void DuplicateResolutionIsNotCountedTwice()
        {
            var journal = new ScenarioJournal("scenario", "run", new[] { "a" }, new[]
            {
                Resolved("a", StepOutcome.Failed), Resolved("a", StepOutcome.Failed)
            });
            var result = new ExamAnalyzer().Analyze(journal);

            Assert.That(result.Score, Is.EqualTo(90));
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void ResolutionWithoutOutcomeDoesNotCompleteStep()
        {
            var result = new ExamAnalyzer().Analyze(Journal(Resolved("a", StepOutcome.None)));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Warnings.Count, Is.EqualTo(2));
        }

        [Test]
        public void FutureSchemaProducesCompatibilityWarning()
        {
            var journal = new ScenarioJournal("scenario", "run", new[] { "a" },
                new[] { Resolved("a", StepOutcome.Completed, 1) }, schemaVersion: 2);
            Assert.That(new TrainingAnalyzer().Analyze(journal).Warnings, Is.Not.Empty);
        }

        [Test]
        public void DuplicateStepIdentifiersAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new ScenarioJournal("scenario", "run",
                new[] { "a", "a" }, Array.Empty<ScenarioEvent>()));
        }

        [Test]
        public void UnknownStepCannotBeLogged()
        {
            var logger = new InMemoryScenarioLogger("scenario", "run", new[] { "a" });
            Assert.Throws<ArgumentException>(() => logger.Record(Resolved("unknown", StepOutcome.Failed)));
        }

        [Test]
        public void InvalidAttemptNumberIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Resolved("a", StepOutcome.Completed, 0));
        }

        [Test]
        public void NullJournalIsRejectedAtAnalyzerBoundary()
        {
            Assert.Throws<ArgumentNullException>(() => new TrainingAnalyzer().Analyze(null));
            Assert.Throws<ArgumentNullException>(() => new ExamAnalyzer().Analyze(null));
        }

        private static ScenarioEvent Resolved(string id, StepOutcome outcome, int? attempt = null)
        {
            return new ScenarioEvent(Timestamp, id, ScenarioEvent.StepResolved, outcome, attempt);
        }

        private static ScenarioJournal Journal(params ScenarioEvent[] entries)
        {
            var ids = new List<string>();
            foreach (var entry in entries)
                ids.Add(entry.StepId);
            return new ScenarioJournal("scenario", "run", ids, entries);
        }
    }
}
