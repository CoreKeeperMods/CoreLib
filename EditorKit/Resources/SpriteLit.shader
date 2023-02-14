Shader "EditorKit/SpriteLit" {
	Properties {
		_OutlineColor ("OutlineColor", Vector) = (0.5188679,0.5188679,0.5188679,1)
		_EmissiveTex ("EmissiveTex", 2D) = "black" {}
		_MainTex ("MainTex", 2D) = "white" {}
		[HDR] _Emissive ("Emissive", Vector) = (0,0,0,0)
		_Tint ("Tint", Vector) = (1,1,1,0)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
	}
	SubShader{
		Tags { 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		LOD 200

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _Color;
			sampler2D _MainTex;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 color = tex2D (_MainTex, IN.texcoord);
				color.rgb *= color.a;
				return color;
			}
		ENDCG
		}
	}
	Fallback "Diffuse"
}