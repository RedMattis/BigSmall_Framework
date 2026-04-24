Shader "BS_TransparentThreeColor"
{
	Properties
  {
		_MainTex ("Texture", 2D) = "white" {}
		_MaskTex ("Texture", 2D) = "black" {}
		_Color ("_Color", Vector) = (1,0,0,1)
		_ColorTwo ("_ColorTwo", Vector) = (0,1,0,1)
		_ColorThree ("_ColorThree", Vector) = (0,0,1,1)
	}
  SubShader
  {
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        
    Pass
		{
      ZWrite Off
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
			sampler2D _MaskTex;

        float4 _Color;
        float4 _ColorTwo;
        float4 _ColorThree;

			v2f vert(appdata v)
			{
				v2f o;
				o.uv = v.uv;
				o.vertex = UnityObjectToClipPos(v.vertex);
				
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);
        clip (col.a - 0.001);
        float col_lumen = dot(col.rgb, float3(0.299, 0.587, 0.114));

				float4 mask = tex2D(_MaskTex, i.uv);
				float r = mask.r;
				float g = mask.g;
				float b = mask.b;
        float mask_alpha = mask.a;

        float total_clr = r + g + b;
        total_clr = max(total_clr, 1.0);
        float3 masked_clr = (( _Color.rgb * r + _ColorTwo.rgb * g + _ColorThree.rgb * b) / total_clr) * col_lumen;
        col.rgb = lerp(col.rgb, masked_clr, mask_alpha);
				return col;
			}
			ENDCG
    }
	}
}