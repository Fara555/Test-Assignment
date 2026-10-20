using System.Reflection;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEngine;

namespace ScenarioConstructor.Editor
{
    internal static class ScenarioConstructorMenu
    {
        [InitializeOnLoadMethod]
        private static void ScheduleMenuCleanup()
        {
            EditorApplication.delayCall += RemoveDefaultTestRunnerMenu;
        }

        private static void RemoveDefaultTestRunnerMenu()
        {
            // Unity exposes menu removal only through its internal Editor API.
            var removeMenuItem = typeof(Menu).GetMethod("RemoveMenuItem",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (removeMenuItem == null)
            {
                Debug.LogWarning("The default Test Runner menu could not be hidden in this Unity version.");
                return;
            }

            removeMenuItem.Invoke(null, new object[] { "Window/General/Test Runner" });
        }

        [MenuItem("Scenario Constructor/Test Runner", priority = 100)]
        private static void OpenTestRunner()
        {
            TestRunnerWindow.ShowWindow();
        }
    }
}
