using System;
using System.Collections.Generic;

internal readonly struct ScriptLookupKey : IEquatable<ScriptLookupKey>
{
    public readonly string AssemblyName;
    public readonly long LocalFileId;

    public ScriptLookupKey(string assemblyName, long localFileId)
    {
        AssemblyName = (assemblyName ?? string.Empty).ToLowerInvariant();
        LocalFileId = localFileId;
    }

    public bool Equals(ScriptLookupKey other)
    {
        return string.Equals(AssemblyName, other.AssemblyName, StringComparison.Ordinal) &&
               LocalFileId == other.LocalFileId;
    }

    public override bool Equals(object obj)
    {
        return obj is ScriptLookupKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((AssemblyName != null ? AssemblyName.GetHashCode() : 0) * 397) ^ LocalFileId.GetHashCode();
        }
    }
}

internal readonly struct ScriptTarget : IEquatable<ScriptTarget>
{
    public readonly string Guid;
    public readonly long LocalFileId;

    public ScriptTarget(string guid, long localFileId)
    {
        Guid = guid;
        LocalFileId = localFileId;
    }

    public bool Equals(ScriptTarget other)
    {
        return string.Equals(Guid, other.Guid, StringComparison.OrdinalIgnoreCase) &&
               LocalFileId == other.LocalFileId;
    }

    public override bool Equals(object obj)
    {
        return obj is ScriptTarget other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Guid != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Guid) : 0) * 397) ^ LocalFileId.GetHashCode();
        }
    }
}

internal readonly struct KnownScriptOverride
{
    public readonly string SourceAssemblyName;
    public readonly string TargetTypeName;

    public KnownScriptOverride(string sourceAssemblyName, string targetTypeName)
    {
        SourceAssemblyName = sourceAssemblyName;
        TargetTypeName = targetTypeName;
    }
}

internal struct RemapStats
{
    public int PrefabCount;
    public int ChangedPrefabCount;
    public int RemappedReferenceCount;
    public int UnresolvedScriptGuidCount;
}
