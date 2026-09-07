using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class PrefabRemapper
{
    [MenuItem("Window/CoreLib Tools/Remap Prefab GUIDs")]
    private static void RemapPrefabGuids()
    {
        string tempDirectory = null;

        try
        {
            EditorUtility.DisplayProgressBar("Remap Prefab GUIDs", "Validating paths...", 0f);

            if (!PrefabGuidRemapPackage.TryGetAssetRipperPath(out string assetRipperPath, out string assetRipperError))
            {
                EditorUtility.DisplayDialog("Remap Prefab GUIDs", assetRipperError, "OK");
                return;
            }

            if (!PrefabGuidRemapPackage.TryGetAssetPackagePath(out string packagePath, out string packageError))
            {
                EditorUtility.DisplayDialog("Remap Prefab GUIDs", packageError, "OK");
                return;
            }

            string projectRoot = PrefabGuidRemapPackage.GetProjectRootPath();
            if (string.IsNullOrEmpty(projectRoot))
            {
                EditorUtility.DisplayDialog("Remap Prefab GUIDs", "Could not resolve project root.", "OK");
                return;
            }

            string ripperPluginsPath = Path.Combine(assetRipperPath, PrefabGuidRemapPackage.GetAssetRipperAssemblyPath()).Replace('\\', '/');
            if (!Directory.Exists(ripperPluginsPath))
            {
                EditorUtility.DisplayDialog(
                    "Remap Prefab GUIDs",
                    $"AssetRipper plugins folder was not found:\n{ripperPluginsPath}",
                    "OK");
                return;
            }

            string projectAssetsPath = Path.Combine(projectRoot, "Assets");
            string projectPackagesPath = Path.Combine(projectRoot, "Packages");

            EditorUtility.DisplayProgressBar("Remap Prefab GUIDs", "Building DLL GUID maps...", 0.08f);

            var ripperGuidsByDllName = PrefabGuidRemapIndex.BuildDllGuidIndex(new[] { ripperPluginsPath });
            var projectGuidsByDllName = PrefabGuidRemapIndex.BuildDllGuidIndex(new[] { projectAssetsPath, projectPackagesPath });

            if (ripperGuidsByDllName.Count == 0)
            {
                EditorUtility.DisplayDialog("Remap Prefab GUIDs", "No DLL meta files were found in the AssetRipper plugins folder.", "OK");
                return;
            }

            if (projectGuidsByDllName.Count == 0)
            {
                EditorUtility.DisplayDialog("Remap Prefab GUIDs", "No DLL meta files were found in this project.", "OK");
                return;
            }

            var dllGuidRemap = PrefabGuidRemapIndex.BuildGuidRemap(ripperGuidsByDllName, projectGuidsByDllName);
            var ripperDllByGuid = PrefabGuidRemapIndex.BuildRipperDllByGuidIndex(ripperGuidsByDllName);

            EditorUtility.DisplayProgressBar("Remap Prefab GUIDs", "Indexing runtime scripts...", 0.15f);
            var runtimeScriptLookup = PrefabGuidRemapScriptLookup.BuildRuntimeScriptLookup(
                out Dictionary<long, ScriptTarget> runtimeScriptLookupByFileId);

            var knownOverrideLookup = PrefabGuidRemapScriptLookup.BuildKnownScriptOverrideLookup();

            var assemblyGuidByName = PrefabGuidRemapIndex.BuildAssemblyDefinitionGuidIndex(new[] { projectAssetsPath, projectPackagesPath });

            var sourceAssemblyNamesByGuid = PrefabGuidRemapIndex.BuildSourceAssemblyNamesByGuid(
                projectGuidsByDllName,
                assemblyGuidByName);

            EditorUtility.DisplayProgressBar("Remap Prefab GUIDs", "Indexing known GUIDs...", 0.2f);
            var knownProjectGuids = PrefabGuidRemapIndex.BuildProjectGuidSet(projectRoot);

            tempDirectory = Path.Combine(Path.GetTempPath(), $"PugModGuidRemap_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDirectory);

            string extractedDirectory = Path.Combine(tempDirectory, "Extracted");
            Directory.CreateDirectory(extractedDirectory);

            EditorUtility.DisplayProgressBar("Remap Prefab GUIDs", "Extracting tarball...", 0.26f);
            TarGzipUtility.ExtractTgz(packagePath, extractedDirectory);

            EditorUtility.DisplayProgressBar("Remap Prefab GUIDs", "Remapping prefab script GUIDs...", 0.4f);
            var remapStats = PrefabGuidRemapProcessor.RemapPrefabScriptGuids(
                extractedDirectory,
                dllGuidRemap,
                ripperDllByGuid,
                runtimeScriptLookup,
                runtimeScriptLookupByFileId,
                knownOverrideLookup,
                sourceAssemblyNamesByGuid,
                assemblyGuidByName,
                knownProjectGuids);

            bool wrotePackage = false;
            if (remapStats.RemappedReferenceCount > 0)
            {
                EditorUtility.DisplayProgressBar("Remap Prefab GUIDs", "Repacking tarball...", 0.87f);
                string repackedPath = PrefabGuidRemapPackage.RepackWithUnityPackageManager(extractedDirectory, tempDirectory);
                File.Copy(repackedPath, packagePath, true);
                wrotePackage = true;

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            EditorUtility.DisplayDialog(
                "Remap Prefab GUIDs",
                wrotePackage
                    ? $"Completed. Updated {remapStats.ChangedPrefabCount} prefabs."
                    : "Completed. Updated 0 prefabs.",
                "OK");

            EditorApplication.delayCall += EditorUtility.RequestScriptReload;

            Debug.Log(
                $"[PrefabRemapper] Completed. " +
                $"Package: {Path.GetFileName(packagePath)}, " +
                $"prefabs scanned: {remapStats.PrefabCount}, changed: {remapStats.ChangedPrefabCount}, " +
                $"remapped refs: {remapStats.RemappedReferenceCount}, unresolved GUIDs: {remapStats.UnresolvedScriptGuidCount}."
            );

            if (remapStats.UnresolvedScriptGuidCount > 0)
            {
                Debug.LogWarning("[PrefabRemapper] Some script GUIDs are still unresolved. Run the remap again after importing any missing assemblies.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Remap Prefab GUIDs", $"Failed: {exception.Message}", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            if (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory))
            {
                PrefabGuidRemapPackage.TryDeleteDirectory(tempDirectory);
            }
        }
    }
}
