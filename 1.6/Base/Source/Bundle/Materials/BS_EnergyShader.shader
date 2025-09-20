Shader "BS_EnergyShader"
{
	Properties
  {
		_MainTex ("Texture", 2D) = "white" {}
		_DrawColor ("DrawColor", Vector) = (1,0,1,1)
	}
  SubShader
  {
		Tags { "Queue"="Transparent"} // "RenderType"="Transparent" 
        
    Pass
		{
      ZWrite Off
      Blend One One

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
        float2 edgeDistV2 = min(i.uv, float2(1.0, 1.0) - i.uv);
        float edgeDist = min(edgeDistV2.x, edgeDistV2.y);
        float edgeFactor = smoothstep(0.0, 0.05, edgeDist);
        
        float2 uv_main = i.uv - float2(0.5, 0.5);
        uv_main.x += sin(_Time.y * 4.5 + i.uv.y * 35.0) * 0.0023 * edgeFactor;
        uv_main.y += cos(-_Time.y * 3.0 + i.uv.x * 29.0) * 0.00185 * edgeFactor;

        float rippleX = sin(uv_main.y * 50.0 + _Time.y * 6.0) * 0.009;
        rippleX += cos(uv_main.x * 60.0 + _Time.y * 4.5) * 0.007;

        float rippleY = +cos(uv_main.x * 50.0 - _Time.y * 5.0) * 0.007;
        rippleY += sin(uv_main.y * 70.0 - _Time.y * 3.5) * 0.0079;
        float2 uv_ripples = uv_main + float2(rippleX, rippleY) * edgeFactor;
        float2 distortionVec = uv_ripples - uv_main;
        float distortionAmount = length(distortionVec);
        float distortDistanceFactor = saturate(1.0 - distortionAmount * 50.0); 

        uv_main =  uv_main * 0.95 + uv_ripples * 0.05; 
        uv_main += float2(0.5, 0.5);
        uv_ripples += float2(0.5, 0.5);
        float4 distort_color = tex2D(_MainTex, uv_ripples);
				float4 main_color = tex2D(_MainTex, uv_main);
        main_color.a *= 0.85;
        main_color.rgb *= main_color.a * _DrawColor.a; // Pre-multiply alpha
        distort_color.a *= distortDistanceFactor * _DrawColor.a;
        distort_color.rgb *= distort_color.a; // Pre-multiply alpha
        
        float4 fused_col = max(main_color, distort_color);
        clip (fused_col.a - 0.001);
        fused_col.rgb *= _DrawColor.rgb;
				return fused_col;
			}
			ENDCG
    }
	}
}