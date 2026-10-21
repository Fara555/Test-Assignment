using System;
using System.Collections.Generic;
using ScenarioConstructor.Unity.Authoring;
using UnityEditor;
using UnityEngine;

namespace ScenarioConstructor.Editor
{
    public static class ExampleConfigurationBuilder
    {
        [MenuItem("Scenario Constructor/Create Example Configuration", priority = 10)]
        public static void CreateExamples()
        {
            const string parent = "Assets/ScenarioConstructor/Examples";
            EnsureFolder(parent);
            var folder = AssetDatabase.GenerateUniqueAssetPath($"{parent}/Inspection");
            CreateAtPath(folder);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<ScenarioProfile>($"{folder}/Training.asset");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        public static void CreateAtPath(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !folder.StartsWith("Assets/", StringComparison.Ordinal)
                || folder.Contains("..") || folder.Contains("\\"))
                throw new ArgumentException("Use a project-relative Assets folder.", nameof(folder));
            if (AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("Example creation never overwrites an existing folder.");
            EnsureFolder(folder);

            var simple = Create<OrderedActionsAsset>(folder, "Perform", asset =>
                SetStrings(asset.FindProperty("actions"), "perform"));
            var sequence = Create<OrderedActionsAsset>(folder, "Enter A Then B", asset =>
                SetStrings(asset.FindProperty("actions"), "enter-a", "enter-b"));
            var gated = Create<StateGatedActionAsset>(folder, "Require Power");
            var titles = new[]
            {
                "Confirm start", "Enter zones A then B", "Touch the inspection wall", "Confirm power is on",
                "Send the custom event", "Check the instrument", "Confirm readings", "Close the inspection",
                "Check channel A", "Check channel B", "Sign the report", "Complete the procedure"
            };
            var steps = new ScenarioStepAsset[titles.Length];
            for (var i = 0; i < steps.Length; i++)
            {
                var index = i;
                steps[i] = Create<ScenarioStepAsset>(folder, $"S{i + 1:00}", asset =>
                {
                    asset.FindProperty("stepId").stringValue = $"S{index + 1:00}";
                    asset.FindProperty("title").stringValue = titles[index];
                    asset.FindProperty("rule").objectReferenceValue = index == 1 ? sequence
                        : index == 3 ? (StepRuleAsset)gated : simple;
                });
            }

            var training = Create<TrainingPolicyAsset>(folder, "Training Execution");
            var exam = Create<ExamPolicyAsset>(folder, "Exam Execution");
            var memory = Create<MemoryLoggerAsset>(folder, "Memory Logger");
            Create<ConsoleLoggerAsset>(folder, "Console Logger");
            var trainingAnalyzer = Create<TrainingAnalyzerAsset>(folder, "Training Analysis");
            var examAnalyzer = Create<ExamAnalyzerAsset>(folder, "Exam Analysis");
            var batches = new[]
            {
                new[] { 0, 1, 2, 3 }, new[] { 4 }, new[] { 5 }, new[] { 6 }, new[] { 7 },
                new[] { 8, 9 }, new[] { 10, 11 }
            };

            CreateProfile(folder, "Training", steps, training, memory, trainingAnalyzer,
                new[] { new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 } }, batches);
            CreateProfile(folder, "Exam", steps, exam, memory, examAnalyzer,
                new[] { new[] { 0, 1, 2, 3 }, new[] { 4, 5, 6, 7 }, new[] { 8, 9, 10, 11 } }, batches);
        }

        private static void CreateProfile(string folder, string name, ScenarioStepAsset[] steps,
            ExecutionPolicyAsset execution, ScenarioLoggerAsset logger, ScenarioAnalyzerAsset analyzer,
            int[][] groups, int[][] batches)
        {
            var profile = Create<ScenarioProfile>(folder, name, asset =>
            {
                asset.FindProperty("scenarioId").stringValue = "inspection";
                var roster = asset.FindProperty("steps");
                roster.arraySize = steps.Length;
                for (var i = 0; i < steps.Length; i++)
                    roster.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];
                SetGroups(asset.FindProperty("groups"), "group", groups, steps);
                SetGroups(asset.FindProperty("activationBatches"), "batch", batches, steps);
                asset.FindProperty("execution").objectReferenceValue = execution;
                asset.FindProperty("logger").objectReferenceValue = logger;
                asset.FindProperty("analyzer").objectReferenceValue = analyzer;
            });
            if (!profile.TryValidate(out var diagnostic))
                throw new InvalidOperationException($"Generated profile '{name}' is invalid: {diagnostic}");
        }

        private static T Create<T>(string folder, string name, Action<SerializedObject> configure = null)
            where T : ScriptableObject
        {
            var result = ScriptableObject.CreateInstance<T>();
            result.name = name;
            if (configure != null)
            {
                var serialized = new SerializedObject(result);
                configure(serialized);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(result, $"{folder}/{name}.asset");
            return result;
        }

        private static void SetStrings(SerializedProperty list, params string[] values)
        {
            list.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                list.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static void SetGroups(SerializedProperty list, string prefix,
            IReadOnlyList<int[]> membership, ScenarioStepAsset[] steps)
        {
            list.arraySize = membership.Count;
            for (var i = 0; i < membership.Count; i++)
            {
                var set = list.GetArrayElementAtIndex(i);
                set.FindPropertyRelative("id").stringValue = $"{prefix}-{i + 1}";
                var members = set.FindPropertyRelative("steps");
                members.arraySize = membership[i].Length;
                for (var j = 0; j < membership[i].Length; j++)
                    members.GetArrayElementAtIndex(j).objectReferenceValue = steps[membership[i][j]];
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
