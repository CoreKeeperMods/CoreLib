using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

internal static class PrefabGuidRemapScriptLookup
{
    internal static Dictionary<ScriptLookupKey, ScriptTarget> BuildRuntimeScriptLookup(
        out Dictionary<long, ScriptTarget> scriptTargetsByFileId)
    {
        var lookup = new Dictionary<ScriptLookupKey, ScriptTarget>();
        var localScriptTargetsByFileId = new Dictionary<long, ScriptTarget>();
        var ambiguousKeys = new HashSet<ScriptLookupKey>();
        var ambiguousFileIds = new HashSet<long>();

        void TryAddLookupEntry(string assemblyFileName, long fileId, ScriptTarget target)
        {
            var key = new ScriptLookupKey(assemblyFileName, fileId);

            if (ambiguousKeys.Contains(key))
            {
                return;
            }

            if (lookup.TryGetValue(key, out ScriptTarget existingTarget))
            {
                if (!existingTarget.Equals(target))
                {
                    // Same (assembly,fileID) resolving to different scripts is unsafe.
                    // Drop it so we fail closed and use other remap strategies.
                    lookup.Remove(key);
                    ambiguousKeys.Add(key);
                }

                return;
            }

            lookup[key] = target;
        }

        void TryAddFileIdLookupEntry(long fileId, ScriptTarget target)
        {
            if (ambiguousFileIds.Contains(fileId))
            {
                return;
            }

            if (localScriptTargetsByFileId.TryGetValue(fileId, out ScriptTarget existingTarget))
            {
                if (!existingTarget.Equals(target))
                {
                    // FileID-only fallback is global and lossy; only keep unique mappings.
                    localScriptTargetsByFileId.Remove(fileId);
                    ambiguousFileIds.Add(fileId);
                }

                return;
            }

            localScriptTargetsByFileId[fileId] = target;
        }

        void TryAddScript(MonoScript script)
        {
            if (script == null)
            {
                return;
            }

            Type type = script.GetClass();
            if (type == null)
            {
                return;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string guid, out long localFileId) || string.IsNullOrEmpty(guid))
            {
                return;
            }

            string assemblyFileName = PrefabGuidRemapIndex.NormalizeAssemblyFileName(type.Assembly.GetName().Name);
            if (string.IsNullOrEmpty(assemblyFileName))
            {
                return;
            }

            var target = new ScriptTarget(guid.ToLowerInvariant(), localFileId);

            TryAddLookupEntry(assemblyFileName, localFileId, target);
            TryAddFileIdLookupEntry(localFileId, target);

            // AssetRipper script refs often use Unity's MD4 type-hash fileID instead of local fileID.
            // Index both forms so either representation can resolve to the same script target.
            if (TryComputeScriptTypeHashFileId(type, out long typeHashFileId) && typeHashFileId != localFileId)
            {
                TryAddLookupEntry(assemblyFileName, typeHashFileId, target);
                TryAddFileIdLookupEntry(typeHashFileId, target);
            }
        }

        MonoScript[] runtimeScripts = MonoImporter.GetAllRuntimeMonoScripts();
        foreach (MonoScript runtimeScript in runtimeScripts)
        {
            TryAddScript(runtimeScript);
        }

        foreach (Type type in EnumerateRuntimeScriptTypes())
        {
            if (!TryGetMonoScriptForType(type, out MonoScript script))
            {
                continue;
            }

            TryAddScript(script);
        }

        scriptTargetsByFileId = localScriptTargetsByFileId;
        return lookup;
    }

    internal static Dictionary<ScriptLookupKey, ScriptTarget> BuildKnownScriptOverrideLookup()
    {
        var lookup = new Dictionary<ScriptLookupKey, ScriptTarget>();

        foreach (KnownScriptOverride knownOverride in PrefabGuidRemapConstants.KnownScriptOverrides)
        {
            if (!TryResolveScriptTargetFromTypeName(knownOverride.TargetTypeName, out ScriptTarget target, out long sourceFileId))
            {
                continue;
            }

            foreach (string candidateAssemblyName in EnumerateAssemblyNameCandidates(knownOverride.SourceAssemblyName))
            {
                AddKnownOverrideLookupEntry(lookup, candidateAssemblyName, sourceFileId, target);
            }
        }

        return lookup;
    }

    internal static bool TryResolveRuntimeScriptTarget(
        IReadOnlyDictionary<ScriptLookupKey, ScriptTarget> runtimeScriptLookup,
        string assemblyName,
        long sourceFileId,
        out ScriptTarget runtimeTarget)
    {
        runtimeTarget = default;

        if (runtimeScriptLookup == null || string.IsNullOrEmpty(assemblyName))
        {
            return false;
        }

        if (!TryGetAlternativeFileIds(sourceFileId, out long signedVariant, out long unsignedVariant))
        {
            signedVariant = sourceFileId;
            unsignedVariant = sourceFileId;
        }

        foreach (string candidateAssemblyName in EnumerateAssemblyNameCandidates(assemblyName))
        {
            if (runtimeScriptLookup.TryGetValue(new ScriptLookupKey(candidateAssemblyName, sourceFileId), out runtimeTarget))
            {
                return true;
            }

            if (signedVariant != sourceFileId &&
                runtimeScriptLookup.TryGetValue(new ScriptLookupKey(candidateAssemblyName, signedVariant), out runtimeTarget))
            {
                return true;
            }

            if (unsignedVariant != sourceFileId &&
                unsignedVariant != signedVariant &&
                runtimeScriptLookup.TryGetValue(new ScriptLookupKey(candidateAssemblyName, unsignedVariant), out runtimeTarget))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool TryResolveRuntimeScriptTargetByFileId(
        IReadOnlyDictionary<long, ScriptTarget> runtimeScriptLookupByFileId,
        long sourceFileId,
        out ScriptTarget runtimeTarget)
    {
        runtimeTarget = default;

        if (runtimeScriptLookupByFileId == null)
        {
            return false;
        }

        if (runtimeScriptLookupByFileId.TryGetValue(sourceFileId, out runtimeTarget))
        {
            return true;
        }

        if (!TryGetAlternativeFileIds(sourceFileId, out long signedVariant, out long unsignedVariant))
        {
            return false;
        }

        if (signedVariant != sourceFileId &&
            runtimeScriptLookupByFileId.TryGetValue(signedVariant, out runtimeTarget))
        {
            return true;
        }

        if (unsignedVariant != sourceFileId &&
            unsignedVariant != signedVariant &&
            runtimeScriptLookupByFileId.TryGetValue(unsignedVariant, out runtimeTarget))
        {
            return true;
        }

        runtimeTarget = default;
        return false;
    }

    internal static bool TryResolveAssemblyGuid(
        IReadOnlyDictionary<string, string> assemblyGuidByName,
        string assemblyName,
        out string assemblyGuid)
    {
        assemblyGuid = string.Empty;
        if (assemblyGuidByName == null || string.IsNullOrEmpty(assemblyName))
        {
            return false;
        }

        foreach (string candidateAssemblyName in EnumerateAssemblyNameCandidates(assemblyName))
        {
            if (assemblyGuidByName.TryGetValue(candidateAssemblyName, out assemblyGuid))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddKnownOverrideLookupEntry(
        IDictionary<ScriptLookupKey, ScriptTarget> lookup,
        string sourceAssemblyName,
        long sourceFileId,
        ScriptTarget target)
    {
        if (string.IsNullOrEmpty(sourceAssemblyName))
        {
            return;
        }

        lookup[new ScriptLookupKey(sourceAssemblyName, sourceFileId)] = target;

        if (!TryGetAlternativeFileIds(sourceFileId, out long signedVariant, out long unsignedVariant))
        {
            return;
        }

        if (signedVariant != sourceFileId)
        {
            lookup[new ScriptLookupKey(sourceAssemblyName, signedVariant)] = target;
        }

        if (unsignedVariant != sourceFileId && unsignedVariant != signedVariant)
        {
            lookup[new ScriptLookupKey(sourceAssemblyName, unsignedVariant)] = target;
        }
    }

    private static IEnumerable<Type> EnumerateRuntimeScriptTypes()
    {
        var seen = new HashSet<Type>();

        foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
        {
            if (IsCandidateRuntimeScriptType(type) && seen.Add(type))
            {
                yield return type;
            }
        }

        foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
        {
            if (IsCandidateRuntimeScriptType(type) && seen.Add(type))
            {
                yield return type;
            }
        }
    }

    private static bool IsCandidateRuntimeScriptType(Type type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.IsAbstract || type.IsGenericTypeDefinition)
        {
            return false;
        }

        return true;
    }

    private static bool TryGetMonoScriptForType(Type type, out MonoScript script)
    {
        script = null;
        if (type == null)
        {
            return false;
        }

        if (PrefabGuidRemapConstants.MonoScriptFromTypeMethod == null)
        {
            return false;
        }

        try
        {
            ParameterInfo[] parameters = PrefabGuidRemapConstants.MonoScriptFromTypeMethod.GetParameters();
            var arguments = new object[parameters.Length];
            arguments[0] = type;

            for (int i = 1; i < parameters.Length; i++)
            {
                if (parameters[i].HasDefaultValue)
                {
                    arguments[i] = parameters[i].DefaultValue;
                    continue;
                }

                Type parameterType = parameters[i].ParameterType;
                arguments[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
            }

            script = PrefabGuidRemapConstants.MonoScriptFromTypeMethod.Invoke(null, arguments) as MonoScript;
        }
        catch
        {
            script = null;
        }

        return script != null;
    }

    private static bool TryResolveScriptTargetFromTypeName(string typeName, out ScriptTarget target, out long sourceFileId)
    {
        target = default;
        sourceFileId = 0;
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        Type resolvedType = null;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            resolvedType = assembly.GetType(typeName, false, false);
            if (resolvedType != null)
            {
                break;
            }
        }

        if (resolvedType == null)
        {
            return false;
        }

        if (!TryComputeScriptTypeHashFileId(resolvedType, out sourceFileId))
        {
            return false;
        }

        if (!TryGetMonoScriptForType(resolvedType, out MonoScript script))
        {
            return false;
        }

        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string guid, out long localFileId) ||
            string.IsNullOrEmpty(guid))
        {
            return false;
        }

        target = new ScriptTarget(guid.ToLowerInvariant(), localFileId);
        return true;
    }

    private static bool TryGetAlternativeFileIds(long fileId, out long signedVariant, out long unsignedVariant)
    {
        signedVariant = fileId;
        unsignedVariant = fileId;

        if (fileId < int.MinValue || fileId > uint.MaxValue)
        {
            return false;
        }

        uint unsignedValue = unchecked((uint)fileId);
        unsignedVariant = unsignedValue;
        signedVariant = unchecked((int)unsignedValue);
        return true;
    }

    private static IEnumerable<string> EnumerateAssemblyNameCandidates(string assemblyName)
    {
        string normalizedAssemblyName = PrefabGuidRemapIndex.NormalizeAssemblyFileName(assemblyName);
        if (string.IsNullOrEmpty(normalizedAssemblyName))
        {
            yield break;
        }

        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool TryYield(string candidate)
        {
            if (string.IsNullOrEmpty(candidate) || !yielded.Add(candidate))
            {
                return false;
            }

            return true;
        }

        if (TryYield(normalizedAssemblyName))
        {
            yield return normalizedAssemblyName;
        }

        string assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(normalizedAssemblyName) ?? string.Empty;
        if (assemblyNameWithoutExtension.EndsWith(".Authoring.Hybrid", StringComparison.OrdinalIgnoreCase))
        {
            string hybridAssemblyName = assemblyNameWithoutExtension.Substring(0, assemblyNameWithoutExtension.Length - ".Authoring.Hybrid".Length) + ".Hybrid.dll";
            if (TryYield(hybridAssemblyName))
            {
                yield return hybridAssemblyName;
            }
        }

        if (assemblyNameWithoutExtension.EndsWith(".Custom", StringComparison.OrdinalIgnoreCase))
        {
            string baseAssemblyName = assemblyNameWithoutExtension.Substring(0, assemblyNameWithoutExtension.Length - ".Custom".Length);

            string nonCustomAssemblyName = baseAssemblyName + ".dll";
            if (TryYield(nonCustomAssemblyName))
            {
                yield return nonCustomAssemblyName;
            }

            string hybridAssemblyName = baseAssemblyName + ".Hybrid.dll";
            if (TryYield(hybridAssemblyName))
            {
                yield return hybridAssemblyName;
            }
        }
    }

    private static bool TryComputeScriptTypeHashFileId(Type type, out long fileId)
    {
        fileId = 0;
        if (type == null || string.IsNullOrEmpty(type.Name))
        {
            return false;
        }

        string namespacePrefix = type.Namespace ?? string.Empty;
        // Unity MonoScript type-hash algorithm input: "s\\0\\0\\0" + namespace + className.
        string hashInput = "s\0\0\0" + namespacePrefix + type.Name;
        using var md4 = MD4.Create();
        byte[] hashBytes = md4.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        if (hashBytes == null || hashBytes.Length < 4)
        {
            return false;
        }

        if (BitConverter.IsLittleEndian)
        {
            fileId = BitConverter.ToInt32(hashBytes, 0);
            return true;
        }

        fileId = (hashBytes[0]) |
                 (hashBytes[1] << 8) |
                 (hashBytes[2] << 16) |
                 (hashBytes[3] << 24);
        return true;
    }
}
