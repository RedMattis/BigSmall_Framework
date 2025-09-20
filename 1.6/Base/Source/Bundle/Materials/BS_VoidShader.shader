Shader "BS_VoidShader"
{
	Properties
  {
		_MainTex ("Texture", 2D) = "white" {}
		_DrawColor ("DrawColor", Vector) = (1,0,1,1)
	}
  SubShader
  {
		Tags {  "Queue"="Transparent"} // "RenderType"="Transparent"
        
    Pass
		{
      Blend DstColor Zero 

      ZWrite Off
      
      //BlendOp RevSub

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
        uv_main.x += sin(_Time.y * 3.4 + i.uv.y * 25.0) * 0.0038 * edgeFactor;
        uv_main.y += cos(_Time.y * 2.6 + i.uv.x * 19.0) * 0.0023 * edgeFactor;

        float rippleX = sin(uv_main.y * 23.0 + _Time.y * 2.0) * 0.022;
        rippleX += cos(uv_main.x * 145.0 + _Time.y * 10.6) * 0.0012;

        float rippleY = cos(uv_main.x * 24.0 + _Time.y * 2.5) * 0.022;
        rippleY += sin(uv_main.y * 120.0 + _Time.y * 12.0) * 0.0012;
        float2 uv_ripples = uv_main + float2(rippleX, rippleY)* edgeFactor;
        float2 distortionVec = uv_ripples - uv_main;
        float distortionAmount = length(distortionVec);
        float distortDistanceFactor = saturate(0.8 - distortionAmount * 5.0); 

        uv_main =  uv_main * 0.95 + uv_ripples * 0.05; // Add slight influence from ripples.

        uv_main += float2(0.5, 0.5);
        uv_ripples += float2(0.5, 0.5);
        float4 distort_color = tex2D(_MainTex, uv_ripples);
				float4 main_color = tex2D(_MainTex, uv_main);
        float main_lumen = dot(main_color.rgb, float3(0.299, 0.587, 0.114));
        //max(main_color.r, max(main_color.g, main_color.b));
        float reduced_lumen = min(main_lumen - 0.15, 0) + main_lumen + 0.15; // Remap so that 0.1-1.0 becomes 0.0-1.0  
        main_lumen = max(saturate(reduced_lumen * (3 - 2 * reduced_lumen)), main_lumen);
        float distort_lumen = dot(distort_color.rgb, float3(0.299, 0.587, 0.114));
        // max(distort_color.r, max(distort_color.g, distort_color.b));
        distort_lumen = saturate(distort_lumen * (3 - 2 * distort_lumen));
        main_color.a *= main_lumen;
        distort_color.a *= distortDistanceFactor * distort_lumen;
        // main_color.a += saturate(distort_color.a*0.06); // Give hint of overlap.
        
        // Pre-multiply alpha
        main_color.rgb *= main_color.a * _DrawColor.a; 
        distort_color.rgb *= distort_color.a * _DrawColor.a;
        
        float4 fused_col = max(main_color, distort_color);
        clip (fused_col.a - 0.0005);
        fused_col *= _DrawColor;
        fused_col.rgb = lerp(float3(1,1,1), fused_col.rgb, fused_col.a);
				return fused_col;
			}
			ENDCG
    }
	}
}