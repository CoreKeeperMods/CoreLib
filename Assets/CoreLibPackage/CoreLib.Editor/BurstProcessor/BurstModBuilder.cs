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

namespace CoreLib.Editor
{
    [Preserve]
    public class BurstModBuilder : PugMod.IPugModBuilderProcessor
    {
        private const string GAME_INSTALL_PATH_KEY = "PugMod/SDKWindow/GamePath";
        private static readonly string[] platforms =
        {
            "Windows", 
            "Linux"
        };

        public void Execute(ModBuilderSettings settings, string installDirectory, List<string> assetPaths)
        {
            if (!settings.GetShouldBuildBurst()) return;
            
            var libraryFolder = Path.Combine(Application.dataPath, "..", "Library");
            var packageCache = Path.Combine(libraryFolder, "PackageCache");
            var burstPackage = Directory.GetDirectories(packageCache).First(s => s.Contains("com.unity.burst"));
            var burstCompiler = Path.Combine(burstPackage, ".Runtime", "bcl.exe");

            var gamePath = EditorPrefs.GetString(GAME_INSTALL_PATH_KEY);
            var gameAssemblies = Path.Combine(gamePath, "CoreKeeper_Data\\Managed");

            var assemblyStaging = Path.Combine(Application.dataPath, "..", "Temp\\ModBurst\\Assemblies");

            if (Directory.Exists(assemblyStaging))
            {
                Directory.Delete(assemblyStaging, true);
            }

            Directory.CreateDirectory(assemblyStaging);

            ScriptCompilationSettings compilationSettings = new ScriptCompilationSettings()
            {
                group = BuildTargetGroup.Standalone,
                target = BuildTarget.StandaloneWindows,
            };

            var compilationResult = PlayerBuildInterface.CompilePlayerScripts(compilationSettings, assemblyStaging);

            if ((compilationResult.assemblies == null ||
                 compilationResult.assemblies.Count == 0) &&
                compilationResult.typeDB == null)
            {
                throw new Exception("Mod scripts build failed!");
            }

            CopyAll(gameAssemblies, assemblyStaging);

            var dlls = assetPaths.Where(path => path.EndsWith(".dll"));

            foreach (string dllPath in dlls)
            {
                var globalDllPath = Path.Combine(Application.dataPath, "..", dllPath);
                var dllName = Path.GetFileName(dllPath);
                
                var destFolder = Path.Combine(assemblyStaging, dllName);
                
                File.Copy(globalDllPath, destFolder, true);
            }

            var burstAssemblyPath = Path.Combine(installDirectory, $"{settings.metadata.name}_burst_generated");
            var rootAssembly = Path.Combine(assemblyStaging, $"{settings.metadata.name}.dll");

            foreach (string platform in platforms)
            {
                if (platform == "Linux" && !settings.GetShouldBuildForLinux()) continue;
                CompileBurst(burstCompiler, assemblyStaging, burstAssemblyPath, rootAssembly, platform);
            }

            Debug.Log("Burst assembly compiled successfully!");
        }

        private static void CompileBurst(
            string burstCompiler, 
            string assemblyStaging, 
            string burstAssemblyPath, 
            string rootAssembly,
            string platform)
        {
            Debug.Log("Starting compiling Burst assembly!");

            Process compiler = new Process();
            compiler.StartInfo.FileName = burstCompiler;
            compiler.StartInfo.Arguments = $"--platform={platform} --target=X64_SSE2 --include-root-assembly-references=False " +
                                           $"--assembly-folder=\"{assemblyStaging}\" --output=\"{burstAssemblyPath}_{platform}\" --root-assembly=\"{rootAssembly}\"";
            compiler.StartInfo.UseShellExecute = false;
            compiler.StartInfo.CreateNoWindow = true;
            compiler.StartInfo.RedirectStandardOutput = true;
            compiler.StartInfo.RedirectStandardError = true;
            compiler.Start();

            compiler.WaitForExit();
            Debug.Log($"Burst compiler output:\n {compiler.StandardOutput.ReadToEnd()}");
        }

        private static void CopyAll(string fromFolder, string toFolder)
        {
            foreach (string newPath in Directory.GetFiles(fromFolder, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(fromFolder, toFolder), true);
            }
        }
    }
}