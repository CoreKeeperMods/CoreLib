using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoreLib.Editor
{
    public static class RenderingFix
    {
        [MenuItem("Window/Core Keeper Tools/Fix rendering", false)]
        public static void FixRendering()
        {
            var rp = Resources.Load<UniversalRenderPipelineAsset>("EditorKit RP");
            Shader spriteShader = Resources.Load<Shader>("SpriteLit");

            Debug.Log("Setting RP asset to EditorKit RP!");
            GraphicsSettings.renderPipelineAsset = rp;

            string[] materialGUIDS = AssetDatabase.FindAssets("t:material");
            string[] materialPaths = materialGUIDS.Select(AssetDatabase.GUIDToAssetPath).ToArray();

            Debug.Log("Updating material shaders!");
            foreach (string materialPath in materialPaths)
            {
                if (!materialPath.Contains("Assets")) continue;
                
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material.shader.name.Contains("Amplify") ||
                    material.shader.name.Contains("Radical"))
                {
                    material.shader = spriteShader;
                }
            }
        }
    }
}