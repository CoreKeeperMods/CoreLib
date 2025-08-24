using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    public static class RenderingFix
    {
        [MenuItem("Window/CoreLib Tools/Fix Rendering", false)]
        public static void FixRendering()
        {
            var rp = Resources.Load<UniversalRenderPipelineAsset>($"EditorKit RP");
            var spriteShader = Resources.Load<Shader>($"SpriteLit");

            Debug.Log("Setting RP asset to EditorKit RP!");
            GraphicsSettings.renderPipelineAsset = rp;

            string[] materialGuids = AssetDatabase.FindAssets("t:material");
            string[] materialPaths = materialGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

            Debug.Log("Updating material shaders!");
            foreach (string materialPath in materialPaths)
            {
                if (!materialPath.Contains("Assets")) continue;
                
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material.shader.name.Contains("Amplify") || material.shader.name.Contains("Radical"))
                    material.shader = spriteShader;
            }
        }
    }
}