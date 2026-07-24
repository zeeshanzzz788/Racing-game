#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Generates the prototype content and builds the Android player in CI.
    /// The output path can be overridden with -buildPath when invoking Unity.
    /// </summary>
    public static class AndroidBuild
    {
        private const string DefaultBuildPath = "build/Android/VelocityRush.apk";

        public static void Build()
        {
            VelocityRushProjectBootstrapper.CreatePrototypeContent();

            string buildPath = GetArgument("-buildPath", DefaultBuildPath);
            string directory = Path.GetDirectoryName(buildPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.StrictMode
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Android build failed with result {report.summary.result} and {report.summary.totalErrors} error(s).");
            }

            Debug.Log($"Android build succeeded: {Path.GetFullPath(buildPath)}");
        }

        private static string[] GetEnabledScenes()
        {
            EditorBuildSettingsScene[] configuredScenes = EditorBuildSettings.scenes;
            List<string> enabledScenes = new List<string>();
            if (configuredScenes != null)
            {
                foreach (EditorBuildSettingsScene scene in configuredScenes)
                {
                    if (scene != null && scene.enabled && !string.IsNullOrEmpty(scene.path))
                    {
                        enabledScenes.Add(scene.path);
                    }
                }
            }

            if (enabledScenes.Count == 0)
            {
                throw new BuildFailedException("No enabled scenes were generated for the Android build.");
            }

            return enabledScenes.ToArray();
        }

        private static string GetArgument(string name, string fallback)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }

            return fallback;
        }
    }
}
#endif
