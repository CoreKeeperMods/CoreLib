using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal static class PrefabGuidRemapConstants
{
    public const string AssetRipperPathKey = "PugMod/SDKWindow/AssetRipperPath";
    public const string AssetPackageName = "dev.pugstorm.corekeeper.assets";
    public const string AssetPackageSearchPattern = "dev.pugstorm.corekeeper.assets-*.tgz";

    public static readonly Regex ManifestPackagePathRegex =
        new Regex("\\\"dev\\.pugstorm\\.corekeeper\\.assets\\\"\\s*:\\s*\\\"file:(?<path>[^\\\"]+\\.tgz)\\\"", RegexOptions.Compiled);

    public static readonly Regex GuidInMetaRegex =
        new Regex("^guid:\\s*([0-9a-fA-F]{32})\\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    public static readonly Regex AsmdefNameRegex =
        new Regex("\\\"name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.Compiled);

    public static readonly Regex ScriptReferenceRegex =
        new Regex("(m_Script:\\s*\\{fileID:\\s*)(-?\\d+)(,\\s*guid:\\s*)([0-9a-fA-F]{32})(\\s*,\\s*type:\\s*3\\s*\\})", RegexOptions.Compiled);

    public static readonly MethodInfo MonoScriptFromTypeMethod =
        typeof(MonoScript)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
            {
                if (!string.Equals(method.Name, "FromType", StringComparison.Ordinal) || method.ReturnType != typeof(MonoScript))
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length > 0 && parameters[0].ParameterType == typeof(Type);
            });

    public static readonly TimeSpan PackTimeout = TimeSpan.FromMinutes(5);

    public static readonly KnownScriptOverride[] KnownScriptOverrides =
    {
        new KnownScriptOverride("Unity.NetCode.Authoring.Hybrid.dll", "Unity.NetCode.Hybrid.GhostAuthoringComponent"),
        new KnownScriptOverride("Unity.NetCode.Authoring.Hybrid.dll", "Unity.NetCode.Hybrid.GhostAuthoringInspectionComponent"),
        new KnownScriptOverride("Unity.Physics.Custom.dll", "Unity.Physics.Authoring.PhysicsShapeAuthoring"),
        new KnownScriptOverride("Unity.Physics.Custom.dll", "Unity.Physics.Authoring.PhysicsBodyAuthoring"),
        new KnownScriptOverride("Unity.Entities.Hybrid.dll", "Unity.Entities.Hybrid.Baking.LinkedEntityGroupAuthoring"),
    };
}
