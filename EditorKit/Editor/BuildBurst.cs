using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildBurst
{
    [MenuItem("Window/Core Keeper Tools/Build Burst Assembly")]
    public static void BuildGame()
    {
        string modName = "BurstTest";

        string projectFolder = Path.Combine(Application.dataPath, "..");
        string buildFolder = Path.Combine(projectFolder, "BurstOutput");

        // Get filename.
        //string path = EditorUtility.SaveFolderPanel("Choose Final Mod Location", "", "");

        FileUtil.DeleteFileOrDirectory(buildFolder);
        Directory.CreateDirectory(buildFolder);

        // Build player.
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Test.unity" }, Path.Combine(buildFolder, $"{modName}.exe"), BuildTarget.StandaloneWindows64, BuildOptions.Development);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Burst assembly built!");
            /*// Copy Managed library
            var managedDest = Path.Combine(path, $"{modName}_Managed.dll");
            var managedSrc = Path.Combine(buildFolder, $"{modName}_Data/Managed/{modName}_Managed.dll");
            FileUtil.DeleteFileOrDirectory(managedDest);
            if (!File.Exists(managedDest))  // Managed side not unloaded
                FileUtil.CopyFileOrDirectory(managedSrc, managedDest);
            else
                Debug.LogWarning($"Couldn't update manged dll, {managedDest} is it currently in use?");

            // Copy Burst library
            var burstedDest = Path.Combine(path, $"{modName}_win_x86_64.dll");
            var burstedSrc = Path.Combine(buildFolder, $"{modName}_Data/Plugins/x86_64/lib_burst_generated.dll");
            FileUtil.DeleteFileOrDirectory(burstedDest);
            if (!File.Exists(burstedDest))
                FileUtil.CopyFileOrDirectory(burstedSrc, burstedDest);
            else
                Debug.LogWarning($"Couldn't update bursted dll, {burstedDest} is it currently in use?");*/
        }
        else
        {
            Debug.LogWarning($"Build failed: errors: {report.summary.totalErrors}");
        }
    }
}
