using System;
using System.Collections.Generic;
using System.Linq;
using QFSW.QC.Utilities;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity
{
    /// <summary>
    /// Provides utility methods and centralized registries for managing and resolving Unity prefab and material assets.
    /// This static class is used to streamline the handling of GameObject prefabs and Materials within the project.
    /// </summary>
    public static class MaterialCrawler
    {
        /// Dictionary that stores material objects mapped by their unique names.
        /// Acts as a centralized registry for materials, enabling efficient lookup and reuse across various components.
        /// This facilitates the management of shared material instances and helps reduce redundancy in material creation.
        public static Dictionary<string, Material> Materials = new();

        /// Boolean flag indicating whether the process of discovering and registering materials has been completed.
        /// Used to prevent redundant executions of material discovery logic and ensure the initialization process is only performed once.
        private static bool _materialsReady;
        
        /// <summary>Event invoked when materials are prepared and ready for swap operations.</summary>
        internal static event Action MaterialSwapReady;
        
        internal static void OnMaterialSwapReady() => MaterialSwapReady?.Invoke();

        internal static void Initialize()
        {
            if (_materialsReady) return;
            var materialArray = Resources.FindObjectsOfTypeAll<Material>()
                .Where(mat => mat.shader.name != "EditorKit/SpriteLit")
                .DistinctBy(mat => mat.name).ToArray();
            Materials = materialArray.ToDictionary(mat => mat.name);
            _materialsReady = true;
            EntityModule.Log.LogInfo($"Material Crawler initialized! Found {Materials.Count} materials.");
        }
    }
}