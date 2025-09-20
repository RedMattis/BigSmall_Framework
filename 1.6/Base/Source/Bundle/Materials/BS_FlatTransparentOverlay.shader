Shader "BS_FlatTransparentOverlay"
{
	Properties
  {
		_MainTex ("Texture", 2D) = "white" {}
		_DrawColor ("DrawColor", Vector) = (1,0,1,1)
	}
  SubShader
  {
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        
    Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
      #include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;

      float4 _DrawColor;

			v2f vert(appdata v)
			{
				v2f o;
				o.uv = v.uv;
				o.vertex = UnityObjectToClipPos(v.vertex);
				
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 _MainTextureColor = tex2D(_MainTex, i.uv);
				float4 col = _MainTextureColor;

        // We only want to overlay "lit" areas, so black/dark areas are also treated as transparent.
        float col_lumen = dot(col.rgb, float3(0.299, 0.587, 0.114));
        float overlayAlpha = col_lumen * col.a * _DrawColor.a;
        clip (overlayAlpha - 0.001);

        col.rgb = _DrawColor.rgb;
        col.a = overlayAlpha;
				return col;
			}
			ENDCG
    }
	}
}