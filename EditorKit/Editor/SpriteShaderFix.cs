using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EditorKit.Editor
{
    public static class SpriteShaderFix
    {
        public static readonly string dummyPass1 =
            "	SubShader{\n" +
            "		Tags { \"RenderType\"=\"Opaque\" }\n" +
            "		LOD 200\n" +
            "		CGPROGRAM\n" +
            "#pragma surface surf Standard\n" +
            "#pragma target 3.0\n" +
            "\n" +
            "		sampler2D _MainTex;\n" +
            "		struct Input\n" +
            "		{\n" +
            "			float2 uv_MainTex;\n" +
            "		};\n" +
            "\n" +
            "		void surf(Input IN, inout SurfaceOutputStandard o)\n" +
            "		{\n" +
            "			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);\n" +
            "			o.Albedo = c.rgb;\n" +
            "			o.Alpha = c.a;\n" +
            "		}\n" +
            "		ENDCG\n" +
            "	}\n";
        
        public static readonly string dummyPass2 =
            "	SubShader{\n" +
            "		Tags { \"RenderType\"=\"Opaque\" }\n" +
            "		LOD 200\n" +
            "		CGPROGRAM\n" +
            "#pragma surface surf Standard\n" +
            "#pragma target 3.0\n" +
            "\n" +
            "		sampler2D _MainTex;\n" +
            "		fixed4 _Color;\n" +
            "		struct Input\n" +
            "		{\n" +
            "			float2 uv_MainTex;\n" +
            "		};\n" +
            "		\n" +
            "		void surf(Input IN, inout SurfaceOutputStandard o)\n" +
            "		{\n" +
            "			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;\n" +
            "			o.Albedo = c.rgb;\n" +
            "			o.Alpha = c.a;\n" +
            "		}\n" +
            "		ENDCG\n" +
            "	}\n";

        public static readonly string fixedPass =
            "	SubShader{\n" +
            "		Tags { \n" +
            "			\"Queue\"=\"Transparent\" \n" +
            "			\"IgnoreProjector\"=\"True\" \n" +
            "			\"RenderType\"=\"Transparent\" \n" +
            "			\"PreviewType\"=\"Plane\"\n" +
            "			\"CanUseSpriteAtlas\"=\"True\"\n" +
            "		}\n" +
            "		LOD 200\n" +
            "\n" +
            "		Cull Off\n" +
            "		Lighting Off\n" +
            "		ZWrite Off\n" +
            "		Blend One OneMinusSrcAlpha\n" +
            "\n" +
            "Pass\n" +
            "		{\n" +
            "		CGPROGRAM\n" +
            "			#pragma vertex vert\n" +
            "			#pragma fragment frag\n" +
            "			#pragma multi_compile _ PIXELSNAP_ON\n" +
            "			#include \"UnityCG.cginc\"\n" +
            "			\n" +
            "			struct appdata_t\n" +
            "			{\n" +
            "				float4 vertex   : POSITION;\n" +
            "				float4 color    : COLOR;\n" +
            "				float2 texcoord : TEXCOORD0;\n" +
            "			};\n" +
            "\n" +
            "			struct v2f\n" +
            "			{\n" +
            "				float4 vertex   : SV_POSITION;\n" +
            "				fixed4 color    : COLOR;\n" +
            "				float2 texcoord  : TEXCOORD0;\n" +
            "			};\n" +
            "			\n" +
            "			fixed4 _Color;\n" +
            "\n" +
            "			v2f vert(appdata_t IN)\n" +
            "			{\n" +
            "				v2f OUT;\n" +
            "				OUT.vertex = UnityObjectToClipPos(IN.vertex);\n" +
            "				OUT.texcoord = IN.texcoord;\n" +
            "				OUT.color = IN.color * _Color;\n" +
            "				#ifdef PIXELSNAP_ON\n" +
            "				OUT.vertex = UnityPixelSnap (OUT.vertex);\n" +
            "				#endif\n" +
            "\n" +
            "				return OUT;\n" +
            "			}\n" +
            "\n" +
            "			sampler2D _MainTex;\n" +
            "			sampler2D _AlphaTex;\n" +
            "			float _AlphaSplitEnabled;\n" +
            "\n" +
            "			fixed4 SampleSpriteTexture (float2 uv)\n" +
            "			{\n" +
            "				fixed4 color = tex2D (_MainTex, uv);\n" +
            "\n" +
            "				return color;\n" +
            "			}\n" +
            "\n" +
            "			fixed4 frag(v2f IN) : SV_Target\n" +
            "			{\n" +
            "				fixed4 color = tex2D (_MainTex, IN.texcoord);\n" +
            "				color.rgb *= color.a;\n" +
            "				return color;\n" +
            "			}\n" +
            "		ENDCG\n" +
            "		}\n" +
            "	}\n";

        [MenuItem("Window/Core Keeper Tools/Fix shaders", false)]
        public static void FixShaders()
        {
            string[] shadersGUIDs = AssetDatabase.FindAssets("t:Shader");
            string[] shaderPaths = shadersGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();

            foreach (string shaderPath in shaderPaths)
            {
                if (!shaderPath.Contains("Assets")) continue;
                string filePath = Path.Combine(Application.dataPath, "..", shaderPath);

                Debug.Log($"Modifying shader {shaderPath}!");
                
                string fileContents = File.ReadAllText(filePath);
                string newFileContents = fileContents
                    .Replace(dummyPass1, fixedPass)
                    .Replace(dummyPass2, fixedPass);
                File.WriteAllText(filePath, newFileContents);
            }
            
            AssetDatabase.Refresh();
        }
    }
}