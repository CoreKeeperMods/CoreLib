using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace CoreLib.Util.Extensions;

public static class ConfigFileExtension
{
    public static Dictionary<ConfigDefinition, string> GetOrphanedEntries(this ConfigFile file)
    {
        PropertyInfo info = typeof(ConfigFile).GetProperty("OrphanedEntries", AccessTools.all);
        if (info != null)
        {
            return (Dictionary<ConfigDefinition, string>)info.GetValue(file);
        }

        throw new Exception("Something went wrong getting ConfigFile.OrphanedEntries property!");
    }
    
}