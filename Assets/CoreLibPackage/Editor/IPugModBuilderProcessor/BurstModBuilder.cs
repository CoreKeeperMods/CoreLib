using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    /// A builder class for handling Burst compilation during the mod building process.
    /// Implements the <c>PugMod.IPugModBuilderProcessor</c> interface for structured mod build processing.
    [Preserve]
    public class BurstModBuilder : PugMod.IPugModBuilderProcessor
    {
        private const string GameInstallPathKey = "PugMod/SDKWindow/GamePath";

        private static readonly string[] SupportedPlatforms =
        {
            "Windows",
            "Linux"
        };

        /// Executes the Burst module building process based on the provided settings, install directory, and asset paths.
        /// <param name="settings">The settings object that contains configuration for the mod building process.</param>
        /// <param name="installDirectory">The directory where the mod will be installed.</param>
        /// <param name="assetPaths">A list of asset paths used during the build process.</param>
        public void Execute(ModBuilderSettings settings, string installDirectory, List<string> assetPaths)
        {
            if (!settings.GetShouldBuildBurst()) return;

            // Resolve important paths
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string libraryDir = Path.Combine(projectRoot, "Library");
            string packageCacheDir = Path.Combine(libraryDir, "PackageCache");
            string burstCompiler = ResolveBurstCompilerPath(packageCacheDir);

            string gamePath = EditorPrefs.GetString(GameInstallPathKey);
            if (string.IsNullOrWhiteSpace(gamePath))
            {
                throw new DirectoryNotFoundException("Game path is not set. Please configure it in the SDK window.");
            }

            string gameAssembliesDir = Path.Combine(gamePath, "CoreKeeper_Data", "Managed");
            if (!Directory.Exists(gameAssembliesDir))
            {
                throw new DirectoryNotFoundException($"Managed assemblies folder not found at: {gameAssembliesDir}");
            }

            string assemblyStagingDir = Path.Combine(projectRoot, "Temp", "ModBurst", "Assemblies");
            PrepareCleanDirectory(assemblyStagingDir);

            // Compile player scripts into staging
            var compilationResult = CompilePlayerAssemblies(assemblyStagingDir);
            ValidateCompilationResult(compilationResult);

            // Copy game assemblies and any dlls referenced by the mod
            CopyAll(gameAssembliesDir, assemblyStagingDir);
            CopyDependencyDlls(assetPaths, projectRoot, assemblyStagingDir);

            // Prepare Burst compilation
            string burstAssemblyBasePath = Path.Combine(installDirectory, $"{settings.metadata.name}_burst_generated");
            string rootAssemblyPath = Path.Combine(assemblyStagingDir, $"{settings.metadata.name}.dll");

            // Compile for selected platforms
            CompileBurstForPlatforms(burstCompiler, assemblyStagingDir, burstAssemblyBasePath, rootAssemblyPath, settings);

            Debug.Log("Burst assembly compiled successfully!");
        }

        /// Resolves the file path to the Burst compiler executable by locating the appropriate package
        /// in the provided package cache directory.
        /// <param name="packageCacheDir">The directory containing cached Unity packages to search for the Burst package.</param>
        /// <returns>The full file path to the Burst compiler executable.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the package cache directory or the Burst package directory cannot be found.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the Burst compiler executable cannot be found within the Burst package directory.</exception>
        private static string ResolveBurstCompilerPath(string packageCacheDir)
        {
            if (!Directory.Exists(packageCacheDir))
            {
                throw new DirectoryNotFoundException($"PackageCache directory not found: {packageCacheDir}");
            }

            string[] burstPackages = Directory.GetDirectories(packageCacheDir, "com.unity.burst*");
            string burstPackageDir = burstPackages
                .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(p => p.IndexOf("com.unity.burst", StringComparison.OrdinalIgnoreCase) >= 0);

            if (string.IsNullOrEmpty(burstPackageDir))
            {
                throw new DirectoryNotFoundException("Unable to find 'com.unity.burst' package in PackageCache.");
            }

            string compilerPath = Path.Combine(burstPackageDir, ".Runtime", "bcl.exe");
            if (!File.Exists(compilerPath))
            {
                throw new FileNotFoundException($"Burst compiler not found at: {compilerPath}");
            }

            return compilerPath;
        }

        /// Prepares a clean directory by deleting its contents if it exists, then recreates it as an empty directory.
        /// <param name="directoryPath">The path of the directory to prepare for clean usage.</param>
        private static void PrepareCleanDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
            Directory.CreateDirectory(directoryPath);
        }

        /// Compiles player assemblies to the specified output directory using the configured build settings.
        /// <param name="outputDirectory">The directory where the compiled player assemblies will be output.</param>
        /// <returns>A <c>ScriptCompilationResult</c> representing the result of the player assembly compilation.</returns>
        private static ScriptCompilationResult CompilePlayerAssemblies(string outputDirectory)
        {
            var compilationSettings = new ScriptCompilationSettings
            {
                group = BuildTargetGroup.Standalone,
                target = BuildTarget.StandaloneWindows,
            };

            return PlayerBuildInterface.CompilePlayerScripts(compilationSettings, outputDirectory);
        }

        private static void ValidateCompilationResult(ScriptCompilationResult result)
        {
            if ((result.assemblies == null || result.assemblies.Count == 0) && result.typeDB == null)
            {
                throw new Exception("Mod scripts build failed!");
            }
        }

        private static void CopyDependencyDlls(IEnumerable<string> assetPaths, string projectRoot, string assemblyStagingDir)
        {
            var dllAssetPaths = assetPaths.Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
            foreach (string dllPath in dllAssetPaths)
            {
                string globalDllPath = Path.Combine(projectRoot, dllPath);
                string dllName = Path.GetFileName(dllPath);
                string destPath = Path.Combine(assemblyStagingDir, dllName);

                if (!File.Exists(globalDllPath))
                {
                    Debug.LogWarning($"Dependency not found, skipping: {globalDllPath}");
                    continue;
                }

                File.Copy(globalDllPath, destPath, true);
            }
        }

        private static void CompileBurstForPlatforms(
            string burstCompiler,
            string assemblyStagingDir,
            string burstAssemblyBasePath,
            string rootAssemblyPath,
            ModBuilderSettings settings)
        {
            foreach (string platform in SupportedPlatforms)
            {
                if (platform == "Linux" && !settings.GetShouldBuildForLinux()) continue;
                CompileBurst(burstCompiler, assemblyStagingDir, burstAssemblyBasePath, rootAssemblyPath, platform);
            }
        }

        private static void CompileBurst(
            string burstCompiler,
            string assemblyStaging,
            string burstAssemblyPath,
            string rootAssembly,
            string platform)
        {
            Debug.Log($"Starting Burst compilation for {platform}...");

            using var compiler = new Process();
            compiler.StartInfo.FileName = burstCompiler;
            compiler.StartInfo.Arguments =
                $"--platform={platform} --target=X64_SSE2 --include-root-assembly-references=False " +
                $"--assembly-folder=\"{assemblyStaging}\" --output=\"{burstAssemblyPath}_{platform}\" --root-assembly=\"{rootAssembly}\"";
            compiler.StartInfo.UseShellExecute = false;
            compiler.StartInfo.CreateNoWindow = true;
            compiler.StartInfo.RedirectStandardOutput = true;
            compiler.StartInfo.RedirectStandardError = true;

            compiler.Start();
            string stdout = compiler.StandardOutput.ReadToEnd();
            string stderr = compiler.StandardError.ReadToEnd();
            compiler.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Debug.Log($"Burst compiler output ({platform}):\n{stdout}");
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Debug.LogWarning($"Burst compiler errors/warnings ({platform}):\n{stderr}");
            }

            if (compiler.ExitCode != 0)
            {
                throw new Exception($"Burst compiler exited with code {compiler.ExitCode} for platform {platform}.");
            }
        }

        private static void CopyAll(string fromFolder, string toFolder)
        {
            string sourceRoot = Path.GetFullPath(fromFolder);
            string targetRoot = Path.GetFullPath(toFolder);

            foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = sourceFile.Substring(sourceRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                string destFile = Path.Combine(targetRoot, relativePath);
                string destDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Copy(sourceFile, destFile, true);
            }
        }
        
    }
}