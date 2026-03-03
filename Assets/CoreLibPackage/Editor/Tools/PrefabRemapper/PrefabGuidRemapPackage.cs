using System;
using System.IO;
using PugMod;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

internal static class PrefabGuidRemapPackage
{
    internal static string GetProjectRootPath()
    {
        return Directory.GetParent(Application.dataPath)?.FullName;
    }

    internal static string GetAssetRipperAssemblyPath()
    {
        var settings = ImporterSettings.Instance;
        if (settings == null || string.IsNullOrEmpty(settings.assetRipperAssembliesPath))
        {
            return "ExportedProject/Assets/Plugins";
        }

        return settings.assetRipperAssembliesPath;
    }

    internal static bool TryGetAssetRipperPath(out string assetRipperPath, out string error)
    {
        assetRipperPath = string.Empty;
        error = string.Empty;

        if (!EditorPrefs.HasKey(PrefabGuidRemapConstants.AssetRipperPathKey))
        {
            error = "No AssetRipper path is assigned in ModSDK.\nOpen Mod SDK Window -> Update Files & Moddable Assets and assign it first.";
            return false;
        }

        string configuredPath = EditorPrefs.GetString(PrefabGuidRemapConstants.AssetRipperPathKey);
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            error = "No AssetRipper path is assigned in ModSDK.\nOpen Mod SDK Window -> Update Files & Moddable Assets and assign it first.";
            return false;
        }

        configuredPath = configuredPath.Trim();

        if (Directory.Exists(Path.Combine(configuredPath, "ExportedProject", "Assets")))
        {
            assetRipperPath = configuredPath;
            return true;
        }

        if (string.Equals(Path.GetFileName(configuredPath), "ExportedProject", StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(Path.Combine(configuredPath, "Assets")))
        {
            assetRipperPath = Directory.GetParent(configuredPath)?.FullName ?? configuredPath;
            return true;
        }

        error =
            $"Configured AssetRipper path is invalid:\n{configuredPath}\n\n" +
            "Open Mod SDK Window -> Update Files & Moddable Assets and assign a valid path.";
        return false;
    }

    internal static bool TryGetAssetPackagePath(out string packagePath, out string error)
    {
        packagePath = string.Empty;
        error = string.Empty;

        string projectRoot = GetProjectRootPath();
        if (string.IsNullOrEmpty(projectRoot))
        {
            error = "Could not resolve project root.";
            return false;
        }

        string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
        if (File.Exists(manifestPath))
        {
            string manifestContent = File.ReadAllText(manifestPath);
            var manifestMatch = PrefabGuidRemapConstants.ManifestPackagePathRegex.Match(manifestContent);
            if (manifestMatch.Success)
            {
                string relativeFilePath = manifestMatch.Groups["path"].Value;
                string absoluteManifestPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestPath) ?? projectRoot, relativeFilePath));

                if (File.Exists(absoluteManifestPath))
                {
                    packagePath = absoluteManifestPath;
                    return true;
                }
            }
        }

        string importedGameFolder = Path.Combine(projectRoot, "ImportedGameFolders");
        if (Directory.Exists(importedGameFolder))
        {
            string[] candidates = Directory.GetFiles(importedGameFolder, PrefabGuidRemapConstants.AssetPackageSearchPattern, SearchOption.TopDirectoryOnly);
            Array.Sort(candidates, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

            if (candidates.Length > 0)
            {
                packagePath = candidates[0];
                return true;
            }
        }

        error =
            $"Could not find {PrefabGuidRemapConstants.AssetPackageName} tarball.\n" +
            "Expected it from Packages/manifest.json or in ImportedGameFolders/.";
        return false;
    }

    internal static string RepackWithUnityPackageManager(string extractedDirectory, string tempDirectory)
    {
        string packageSourceDirectory = ResolvePackageSourceDirectory(extractedDirectory);
        string packOutputDirectory = Path.Combine(tempDirectory, "UnityPack");
        Directory.CreateDirectory(packOutputDirectory);

        PackRequest packRequest = Client.Pack(packageSourceDirectory, packOutputDirectory);
        DateTime startedAt = DateTime.UtcNow;
        DateTime timeoutAt = DateTime.UtcNow + PrefabGuidRemapConstants.PackTimeout;
        float progressStart = 0.87f;
        float progressRange = 0.11f;

        while (!packRequest.IsCompleted)
        {
            if (DateTime.UtcNow >= timeoutAt)
            {
                throw new TimeoutException($"Timed out waiting for Unity Package Manager pack after {PrefabGuidRemapConstants.PackTimeout.TotalMinutes:0} minutes.");
            }

            float elapsedSeconds = (float)(DateTime.UtcNow - startedAt).TotalSeconds;
            float simulatedProgress = Mathf.Clamp01(elapsedSeconds / (float)PrefabGuidRemapConstants.PackTimeout.TotalSeconds);
            EditorUtility.DisplayProgressBar(
                "Remap Prefab GUIDs",
                "Repacking tarball with Unity Package Manager...",
                progressStart + (progressRange * simulatedProgress));
            System.Threading.Thread.Sleep(25);
        }

        if (packRequest.Status != StatusCode.Success || packRequest.Result == null || string.IsNullOrEmpty(packRequest.Result.tarballPath))
        {
            string errorMessage = packRequest.Error != null ? packRequest.Error.message : "Unknown error.";
            throw new InvalidOperationException($"Unity Package Manager failed to pack tgz: {errorMessage}");
        }

        if (!File.Exists(packRequest.Result.tarballPath))
        {
            throw new FileNotFoundException("Unity Package Manager reported a tarball path that does not exist.", packRequest.Result.tarballPath);
        }

        return packRequest.Result.tarballPath;
    }

    internal static void TryDeleteDirectory(string directoryPath)
    {
        try
        {
            Directory.Delete(directoryPath, true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[PrefabRemapper] Failed to delete temp directory {directoryPath}: {exception.Message}");
        }
    }

    private static string ResolvePackageSourceDirectory(string extractedDirectory)
    {
        string rootPackageJson = Path.Combine(extractedDirectory, "package.json");
        if (File.Exists(rootPackageJson))
        {
            return extractedDirectory;
        }

        string packageSubdirectory = Path.Combine(extractedDirectory, "package");
        if (File.Exists(Path.Combine(packageSubdirectory, "package.json")))
        {
            return packageSubdirectory;
        }

        string[] candidates = Directory.GetFiles(extractedDirectory, "package.json", SearchOption.AllDirectories);
        if (candidates.Length == 1)
        {
            string directory = Path.GetDirectoryName(candidates[0]);
            if (!string.IsNullOrEmpty(directory))
            {
                return directory;
            }
        }

        throw new InvalidDataException("Could not locate package.json in extracted tarball contents.");
    }
}
