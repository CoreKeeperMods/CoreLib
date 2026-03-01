using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class PrefabGuidRemapIndex
{
    internal static Dictionary<string, string> BuildDllGuidIndex(IEnumerable<string> rootDirectories)
    {
        var guidByDllName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var duplicateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rootDirectory in rootDirectories)
        {
            if (string.IsNullOrEmpty(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                continue;
            }

            foreach (string metaPath in Directory.GetFiles(rootDirectory, "*.dll.meta", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileNameWithoutExtension(metaPath);

                if (!TryReadGuidFromMeta(metaPath, out string guid))
                {
                    continue;
                }

                if (duplicateNames.Contains(fileName))
                {
                    continue;
                }

                if (guidByDllName.TryGetValue(fileName, out string existingGuid))
                {
                    if (!string.Equals(existingGuid, guid, StringComparison.OrdinalIgnoreCase))
                    {
                        guidByDllName.Remove(fileName);
                        duplicateNames.Add(fileName);
                    }

                    continue;
                }

                guidByDllName[fileName] = guid;
            }
        }

        return guidByDllName;
    }

    internal static Dictionary<string, string> BuildGuidRemap(
        IReadOnlyDictionary<string, string> ripperGuidsByDllName,
        IReadOnlyDictionary<string, string> projectGuidsByDllName)
    {
        var remap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> pair in ripperGuidsByDllName)
        {
            if (!projectGuidsByDllName.TryGetValue(pair.Key, out string targetGuid))
            {
                continue;
            }

            string sourceGuid = pair.Value.ToLowerInvariant();
            string destinationGuid = targetGuid.ToLowerInvariant();

            if (!string.Equals(sourceGuid, destinationGuid, StringComparison.Ordinal))
            {
                remap[sourceGuid] = destinationGuid;
            }
        }

        return remap;
    }

    internal static Dictionary<string, string> BuildAssemblyDefinitionGuidIndex(IEnumerable<string> rootDirectories)
    {
        var guidByAssemblyName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var duplicateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rootDirectory in rootDirectories)
        {
            if (string.IsNullOrEmpty(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                continue;
            }

            foreach (string asmdefPath in Directory.GetFiles(rootDirectory, "*.asmdef", SearchOption.AllDirectories))
            {
                string metaPath = asmdefPath + ".meta";
                if (!File.Exists(metaPath) || !TryReadGuidFromMeta(metaPath, out string guid))
                {
                    continue;
                }

                string asmdefContent = File.ReadAllText(asmdefPath);
                var nameMatch = PrefabGuidRemapConstants.AsmdefNameRegex.Match(asmdefContent);
                if (!nameMatch.Success)
                {
                    continue;
                }

                string assemblyName = NormalizeAssemblyFileName(nameMatch.Groups[1].Value);
                if (string.IsNullOrEmpty(assemblyName))
                {
                    continue;
                }

                if (duplicateNames.Contains(assemblyName))
                {
                    continue;
                }

                if (guidByAssemblyName.TryGetValue(assemblyName, out string existingGuid))
                {
                    if (!string.Equals(existingGuid, guid, StringComparison.OrdinalIgnoreCase))
                    {
                        guidByAssemblyName.Remove(assemblyName);
                        duplicateNames.Add(assemblyName);
                    }

                    continue;
                }

                guidByAssemblyName[assemblyName] = guid.ToLowerInvariant();
            }
        }

        return guidByAssemblyName;
    }

    internal static Dictionary<string, string> BuildRipperDllByGuidIndex(IReadOnlyDictionary<string, string> ripperGuidsByDllName)
    {
        var ripperDllByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> pair in ripperGuidsByDllName)
        {
            string guid = pair.Value.ToLowerInvariant();
            if (!ripperDllByGuid.ContainsKey(guid))
            {
                ripperDllByGuid[guid] = pair.Key;
            }
        }

        return ripperDllByGuid;
    }

    internal static Dictionary<string, List<string>> BuildSourceAssemblyNamesByGuid(
        IReadOnlyDictionary<string, string> projectGuidsByDllName,
        IReadOnlyDictionary<string, string> assemblyGuidByName)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        void Add(string guid, string assemblyName)
        {
            if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(assemblyName))
            {
                return;
            }

            string normalizedGuid = guid.ToLowerInvariant();
            string normalizedAssemblyName = NormalizeAssemblyFileName(assemblyName);
            if (string.IsNullOrEmpty(normalizedAssemblyName))
            {
                return;
            }

            if (!map.TryGetValue(normalizedGuid, out HashSet<string> names))
            {
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[normalizedGuid] = names;
            }

            names.Add(normalizedAssemblyName);
        }

        foreach (KeyValuePair<string, string> pair in projectGuidsByDllName)
        {
            Add(pair.Value, pair.Key);
        }

        foreach (KeyValuePair<string, string> pair in assemblyGuidByName)
        {
            Add(pair.Value, pair.Key);
        }

        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, HashSet<string>> pair in map)
        {
            result[pair.Key] = pair.Value.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        return result;
    }

    internal static HashSet<string> BuildProjectGuidSet(string projectRoot)
    {
        var knownGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string[] roots =
        {
            Path.Combine(projectRoot, "Assets"),
            Path.Combine(projectRoot, "Packages"),
        };

        foreach (string root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string metaPath in Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories))
            {
                if (TryReadGuidFromMeta(metaPath, out string guid))
                {
                    knownGuids.Add(guid.ToLowerInvariant());
                }
            }
        }

        return knownGuids;
    }

    internal static bool TryReadGuidFromMeta(string metaPath, out string guid)
    {
        guid = string.Empty;

        string content = File.ReadAllText(metaPath);
        var match = PrefabGuidRemapConstants.GuidInMetaRegex.Match(content);
        if (!match.Success)
        {
            return false;
        }

        guid = match.Groups[1].Value;
        return true;
    }

    internal static string NormalizeAssemblyFileName(string assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return string.Empty;
        }

        string trimmedName = assemblyName.Trim();
        if (!trimmedName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            trimmedName += ".dll";
        }

        return trimmedName;
    }
}
