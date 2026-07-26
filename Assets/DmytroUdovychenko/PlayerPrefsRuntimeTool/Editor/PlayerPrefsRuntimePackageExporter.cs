// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================

#if PLAYER_PREFS_RUNTIME_TOOL
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DmytroUdovychenko.PlayerPrefsRuntimeTool.Editor
{
    /// <summary>
    /// Exports the tool as a versioned <c>.unitypackage</c>. Available from the Tools menu and as a
    /// static entry point for Unity batch mode:
    /// <code>
    /// Unity -batchmode -quit -projectPath . \
    ///   -executeMethod DmytroUdovychenko.PlayerPrefsRuntimeTool.Editor.PlayerPrefsRuntimePackageExporter.Export \
    ///   -exportPath ./PlayerPrefsRuntimeTool.unitypackage
    /// </code>
    /// The <c>-exportPath</c> argument is optional; without it the package is written to the project
    /// root as <c>PlayerPrefsRuntimeTool-&lt;version&gt;.unitypackage</c>.
    /// </summary>
    internal static class PlayerPrefsRuntimePackageExporter
    {
        private const string AssetRoot = "Assets/DmytroUdovychenko/PlayerPrefsRuntimeTool";
        private const string MenuPath = "Tools/PlayerPrefs Runtime Viewer/Export .unitypackage";
        private const string ExportPathArg = "-exportPath";

        [MenuItem(MenuPath)]
        private static void ExportFromMenu()
        {
            string path = EditorUtility.SaveFilePanel(
                "Export PlayerPrefsRuntime Tool",
                GetProjectRoot(),
                DefaultFileName(),
                "unitypackage");

            if (string.IsNullOrEmpty(path))
            {
                return; // user cancelled
            }

            ExportTo(path);
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>
        /// Batch-mode entry point. Writes to the path given by <c>-exportPath</c>, or to
        /// <c>&lt;projectRoot&gt;/&lt;default name&gt;</c> when the argument is absent.
        /// </summary>
        public static void Export()
        {
            string path = GetExportPathArg() ?? Path.Combine(GetProjectRoot(), DefaultFileName());
            ExportTo(path);
        }

        private static void ExportTo(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Recurse pulls in the whole tool folder; dependencies (Newtonsoft) are intentionally
            // not bundled so they resolve through the Package Manager on the importing project.
            AssetDatabase.ExportPackage(AssetRoot, path, ExportPackageOptions.Recurse);
            Debug.Log($"[PlayerPrefsRuntime] Exported package to {path}");
        }

        private static string DefaultFileName()
        {
            return $"PlayerPrefsRuntimeTool-{PlayerPrefsRuntime.Version}.unitypackage";
        }

        private static string GetProjectRoot()
        {
            // Application.dataPath ends with "/Assets"; the project root is its parent directory.
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string GetExportPathArg()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ExportPathArg)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
#endif
