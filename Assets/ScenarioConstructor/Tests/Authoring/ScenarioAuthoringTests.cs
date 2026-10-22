using System;
using System.Linq;
using NUnit.Framework;
using ScenarioConstructor.Conditions;
using ScenarioConstructor.Editor;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Unity.Authoring;
using ScenarioConstructor.Unity.Scene;
using UnityEditor;
using UnityEngine;

namespace ScenarioConstructor.Tests
{
    public sealed class ScenarioAuthoringTests
    {
        private string folder;
        private ScenarioProfile training;
        private ScenarioProfile exam;

        [OneTimeSetUp]
        public void CreateFixtureAssets()
        {
            folder = $"Assets/__ScenarioAuthoringTests_{Guid.NewGuid():N}";
            ExampleConfigurationBuilder.CreateAtPath(folder);
        }

        [SetUp]
        public void CopyProfiles()
        {
            training = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ScenarioProfile>($"{folder}/Training.asset"));
            exam = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ScenarioProfile>($"{folder}/Exam.asset"));
        }

        [TearDown]
        public void DestroyCopies()
        {
            UnityEngine.Object.DestroyImmediate(training);
            UnityEngine.Object.DestroyImmediate(exam);
        }

        [OneTimeTearDown]
        public void DeleteFixtureAssets() => AssetDatabase.DeleteAsset(folder);

        [Test]
        public void ExecutorFactoryCanReplaceTheConcreteRunnerWithoutDuplicatingSteps()
        {
            var replacement = UnityEngine.ScriptableObject.CreateInstance<ReplacementExecutionAsset>();
            try
            {
                var originalStep = training.Steps[0];
                SetReference(training, "execution", replacement);
                var execution = training.CreateRunner();
                Assert.That(execution, Is.Not.TypeOf<ScenarioRunner>());
                execution.Submit("S02", new StepSignal("enter-b"));
                Assert.That(execution.GetProgress("S02").Status, Is.EqualTo(StepStatus.Failed));
                Assert.That(execution.GetProgress("S01").Status, Is.EqualTo(StepStatus.Skipped));
                Assert.That(training.Steps[0], Is.SameAs(originalStep));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(replacement);
            }
        }

        [Test]
        public void SerializedExamplesAreValidAndShareAllTwelveStepAssets()
        {
            Assert.That(training.TryValidate(out var trainingDiagnostic), Is.True, trainingDiagnostic);
            Assert.That(exam.TryValidate(out var examDiagnostic), Is.True, examDiagnostic);
            Assert.That(training.Steps.Count, Is.EqualTo(12));
            for (var i = 0; i < training.Steps.Count; i++)
                Assert.That(training.Steps[i], Is.SameAs(exam.Steps[i]));
            Assert.That(training.BuildPlan().Groups.Count, Is.EqualTo(1));
            Assert.That(exam.BuildPlan().Groups.Count, Is.EqualTo(3));
        }

        [Test]
        public void ActivationBatchesCanBeReorderedThroughSerializedPropertiesWithUndo()
        {
            var serialized = new SerializedObject(training);
            serialized.FindProperty("activationBatches").MoveArrayElement(1, 0);
            serialized.ApplyModifiedProperties();
            Assert.That(training.CreateRunner().GetProgress("S05").Status, Is.EqualTo(StepStatus.Active));
            Undo.PerformUndo();
            Assert.That(training.CreateRunner().GetProgress("S01").Status, Is.EqualTo(StepStatus.Active));
        }

        [Test]
        public void MissingLoggerProducesAuthoringDiagnostic()
        {
            SetReference(training, "logger", null);
            Assert.That(training.TryValidate(out var diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("logger"));
        }

        [Test]
        public void StepAssetOutsideRosterIsRejectedEvenIfItsIdentifierMatches()
        {
            var duplicate = UnityEngine.Object.Instantiate(training.Steps[0]);
            try
            {
                var serialized = new SerializedObject(training);
                serialized.FindProperty("groups").GetArrayElementAtIndex(0).FindPropertyRelative("steps")
                    .GetArrayElementAtIndex(0).objectReferenceValue = duplicate;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(training.TryValidate(out var diagnostic), Is.False);
                Assert.That(diagnostic, Does.Contain("roster"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(duplicate);
            }
        }

        [Test]
        public void AnalyzerCanBeSwitchedWithoutChangingStepsOrExecution()
        {
            var runner = training.CreateRunner();
            var before = training.BuildPlan();
            SetReference(training, "analyzer", AssetDatabase.LoadAssetAtPath<ExamAnalyzerAsset>($"{folder}/Exam Analysis.asset"));
            Assert.That(training.Analyzer.CreateAnalyzer().Analyze(runner.CaptureJournal()).MaximumScore, Is.EqualTo(100));
            Assert.That(training.BuildPlan().Steps.Select(step => step.Id), Is.EqualTo(before.Steps.Select(step => step.Id)));
            Assert.That(training.CreateRunner().GetProgress("S01").Status, Is.EqualTo(StepStatus.Active));
        }

        [Test]
        public void LoggerCanBeSwitchedIndependentlyThroughSerializedReference()
        {
            SetReference(training, "logger", AssetDatabase.LoadAssetAtPath<ConsoleLoggerAsset>($"{folder}/Console Logger.asset"));
            var runner = training.CreateRunner();
            runner.Submit("S01", new StepSignal("perform"));
            Assert.That(runner.GetProgress("S01").Status, Is.EqualTo(StepStatus.Completed));
            Assert.That(runner.CaptureJournal().Events.Last().Outcome, Is.EqualTo(Journal.StepOutcome.Completed));
        }

        [Test]
        public void ExamThresholdCanBeEditedWithoutCode()
        {
            var policy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ExamPolicyAsset>($"{folder}/Exam Execution.asset"));
            try
            {
                var serialized = new SerializedObject(policy);
                serialized.FindProperty("groupViolationLimit").intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                SetReference(exam, "execution", policy);
                var runner = exam.CreateRunner();
                runner.Submit("S02", new StepSignal("enter-b"));
                Assert.That(runner.GetProgress("S01").Status, Is.EqualTo(StepStatus.Skipped));
                Assert.That(runner.CurrentGroupId, Is.EqualTo("group-2"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(policy);
            }
        }

        [Test]
        public void GeneratedConfigurationDoesNotOverwriteExistingAssets()
        {
            Assert.Throws<InvalidOperationException>(() => ExampleConfigurationBuilder.CreateAtPath(folder));
        }

        [Test]
        public void PendingStepPolicyCanBeChangedInInspector()
        {
            var serialized = new SerializedObject(training);
            serialized.FindProperty("pendingActions").enumValueIndex = (int)PendingActionBehavior.Ignore;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var runner = training.CreateRunner();
            Assert.That(runner.Submit("S05", new StepSignal("perform")).Status, Is.EqualTo(ConditionStatus.Ignored));
            Assert.That(runner.GetProgress("S05").Violations, Is.Zero);
        }

        [TestCase(0, "perform", true)]
        [TestCase(1, "enter-a", true)]
        [TestCase(1, "enter-b", true)]
        [TestCase(0, "perfrom", false)]
        [TestCase(0, "Perform", false)]
        public void RelayValidatesActionAgainstSelectedStepRule(int stepIndex, string actionId, bool expected)
        {
            var step = training.Steps[stepIndex];
            Assert.That(ValidateRelay(step, actionId, out var diagnostic), Is.EqualTo(expected), diagnostic);
            if (!expected)
            {
                Assert.That(diagnostic, Does.Contain(actionId));
                Assert.That(diagnostic, Does.Contain(step.StepId));
            }
        }

        [Test]
        public void RelayReportsInvalidStepConfigurationWithoutThrowing()
        {
            var step = UnityEngine.Object.Instantiate(training.Steps[0]);
            try
            {
                SetReference(step, "rule", null);
                Assert.That(ValidateRelay(step, "perform", out var diagnostic), Is.False);
                Assert.That(diagnostic, Does.Contain("condition asset"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(step);
            }
        }

        private static bool ValidateRelay(ScenarioStepAsset step, string actionId, out string diagnostic)
        {
            var root = new GameObject("Action validation fixture");
            root.SetActive(false);
            try
            {
                var session = root.AddComponent<ScenarioSession>();
                var relay = root.AddComponent<ScenarioActionRelay>();
                SetReference(relay, "session", session);
                SetReference(relay, "step", step);
                var settings = new SerializedObject(relay);
                settings.FindProperty("actionId").stringValue = actionId;
                settings.ApplyModifiedPropertiesWithoutUndo();
                return relay.TryValidate(out diagnostic);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
