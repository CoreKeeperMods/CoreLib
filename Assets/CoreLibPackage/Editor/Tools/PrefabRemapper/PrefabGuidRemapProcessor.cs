using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;

internal static class PrefabGuidRemapProcessor
{
    internal static RemapStats RemapPrefabScriptGuids(
        string extractedRootPath,
        IReadOnlyDictionary<string, string> dllGuidRemap,
        IReadOnlyDictionary<string, string> ripperDllByGuid,
        IReadOnlyDictionary<ScriptLookupKey, ScriptTarget> runtimeScriptLookup,
        IReadOnlyDictionary<long, ScriptTarget> runtimeScriptLookupByFileId,
        IReadOnlyDictionary<ScriptLookupKey, ScriptTarget> knownOverrideLookup,
        IReadOnlyDictionary<string, List<string>> sourceAssemblyNamesByGuid,
        IReadOnlyDictionary<string, string> assemblyGuidByName,
        ISet<string> knownProjectGuids)
    {
        string[] prefabPaths = Directory.GetFiles(extractedRootPath, "*.prefab", SearchOption.AllDirectories);
        var unresolvedScriptGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int changedPrefabCount = 0;
        int remappedReferenceCount = 0;

        for (int index = 0; index < prefabPaths.Length; index++)
        {
            string prefabPath = prefabPaths[index];

            float progress = 0.4f + (0.45f * (index + 1) / Math.Max(1, prefabPaths.Length));
            EditorUtility.DisplayProgressBar("Remap Prefab GUIDs", $"Processing prefabs ({index + 1}/{prefabPaths.Length})...", progress);

            string originalContent = File.ReadAllText(prefabPath);
            int replacementsInPrefab = 0;

            string remappedContent = PrefabGuidRemapConstants.ScriptReferenceRegex.Replace(originalContent, match =>
            {
                if (!long.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long sourceFileId))
                {
                    return match.Value;
                }

                string sourceGuid = match.Groups[4].Value.ToLowerInvariant();
                string destinationGuid = sourceGuid;
                long destinationFileId = sourceFileId;
                bool changed = false;

                bool knowsSourceAssembly = ripperDllByGuid.TryGetValue(sourceGuid, out string ripperDllName);
                if (knowsSourceAssembly)
                {
                    // Resolution order: exact runtime script -> explicit override -> fileID-only runtime fallback
                    // -> dll GUID remap -> asmdef assembly GUID fallback -> unresolved tracking.
                    if (PrefabGuidRemapScriptLookup.TryResolveRuntimeScriptTarget(runtimeScriptLookup, ripperDllName, sourceFileId, out ScriptTarget runtimeTarget))
                    {
                        destinationGuid = runtimeTarget.Guid;
                        destinationFileId = runtimeTarget.LocalFileId;
                        changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase) ||
                                  sourceFileId != destinationFileId;
                    }
                    else if (knownOverrideLookup.TryGetValue(new ScriptLookupKey(ripperDllName, sourceFileId), out ScriptTarget knownOverrideTarget))
                    {
                        destinationGuid = knownOverrideTarget.Guid;
                        destinationFileId = knownOverrideTarget.LocalFileId;
                        changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase) ||
                                  sourceFileId != destinationFileId;
                    }
                    else if (PrefabGuidRemapScriptLookup.TryResolveRuntimeScriptTargetByFileId(runtimeScriptLookupByFileId, sourceFileId, out ScriptTarget runtimeFileIdTarget))
                    {
                        destinationGuid = runtimeFileIdTarget.Guid;
                        destinationFileId = runtimeFileIdTarget.LocalFileId;
                        changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase) ||
                                  sourceFileId != destinationFileId;
                    }
                    else if (dllGuidRemap.TryGetValue(sourceGuid, out string mappedGuid))
                    {
                        destinationGuid = mappedGuid;
                        changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (PrefabGuidRemapScriptLookup.TryResolveAssemblyGuid(assemblyGuidByName, ripperDllName, out string assemblyGuid))
                    {
                        destinationGuid = assemblyGuid;
                        changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (!knownProjectGuids.Contains(sourceGuid))
                    {
                        unresolvedScriptGuids.Add(sourceGuid);
                    }
                }
                else if (dllGuidRemap.TryGetValue(sourceGuid, out string mappedGuid))
                {
                    destinationGuid = mappedGuid;
                    changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase);
                }
                else if (sourceAssemblyNamesByGuid.TryGetValue(sourceGuid, out List<string> sourceAssemblyCandidates) &&
                         sourceAssemblyCandidates.Count > 0)
                {
                    // Some source GUIDs are already project GUIDs (for assemblies/scripts); use that to recover
                    // candidate assembly names and still attempt script-level remap.
                    bool remappedFromProjectAssemblyGuid = false;
                    foreach (string assemblyCandidate in sourceAssemblyCandidates)
                    {
                        if (PrefabGuidRemapScriptLookup.TryResolveRuntimeScriptTarget(runtimeScriptLookup, assemblyCandidate, sourceFileId, out ScriptTarget runtimeTarget))
                        {
                            destinationGuid = runtimeTarget.Guid;
                            destinationFileId = runtimeTarget.LocalFileId;
                            changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase) ||
                                      sourceFileId != destinationFileId;
                            remappedFromProjectAssemblyGuid = true;
                            break;
                        }

                        if (knownOverrideLookup.TryGetValue(new ScriptLookupKey(assemblyCandidate, sourceFileId), out ScriptTarget knownOverrideTarget))
                        {
                            destinationGuid = knownOverrideTarget.Guid;
                            destinationFileId = knownOverrideTarget.LocalFileId;
                            changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase) ||
                                      sourceFileId != destinationFileId;
                            remappedFromProjectAssemblyGuid = true;
                            break;
                        }
                    }

                    if (!remappedFromProjectAssemblyGuid)
                    {
                        if (PrefabGuidRemapScriptLookup.TryResolveRuntimeScriptTargetByFileId(runtimeScriptLookupByFileId, sourceFileId, out ScriptTarget runtimeFileIdTarget))
                        {
                            destinationGuid = runtimeFileIdTarget.Guid;
                            destinationFileId = runtimeFileIdTarget.LocalFileId;
                            changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase) ||
                                      sourceFileId != destinationFileId;
                            remappedFromProjectAssemblyGuid = true;
                        }
                    }

                    if (!remappedFromProjectAssemblyGuid)
                    {
                        if (!knownProjectGuids.Contains(sourceGuid))
                        {
                            unresolvedScriptGuids.Add(sourceGuid);
                        }
                    }
                }
                else if (!knownProjectGuids.Contains(sourceGuid))
                {
                    if (PrefabGuidRemapScriptLookup.TryResolveRuntimeScriptTargetByFileId(runtimeScriptLookupByFileId, sourceFileId, out ScriptTarget runtimeFileIdTarget))
                    {
                        destinationGuid = runtimeFileIdTarget.Guid;
                        destinationFileId = runtimeFileIdTarget.LocalFileId;
                        changed = !string.Equals(sourceGuid, destinationGuid, StringComparison.OrdinalIgnoreCase) ||
                                  sourceFileId != destinationFileId;
                    }
                    else
                    {
                        unresolvedScriptGuids.Add(sourceGuid);
                    }
                }

                if (!changed)
                {
                    return match.Value;
                }

                replacementsInPrefab++;
                remappedReferenceCount++;

                return match.Groups[1].Value +
                       destinationFileId.ToString(CultureInfo.InvariantCulture) +
                       match.Groups[3].Value +
                       destinationGuid +
                       match.Groups[5].Value;
            });

            if (replacementsInPrefab > 0)
            {
                File.WriteAllText(prefabPath, remappedContent);
                changedPrefabCount++;
            }
        }

        return new RemapStats
        {
            PrefabCount = prefabPaths.Length,
            ChangedPrefabCount = changedPrefabCount,
            RemappedReferenceCount = remappedReferenceCount,
            UnresolvedScriptGuidCount = unresolvedScriptGuids.Count,
        };
    }
}
