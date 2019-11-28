Shader "Hidden/AveragedMultiply"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Traced ("Texture", 2D) = "white" {}
		_InvGamma("Inverse Gamma", Float) = 2.2
		_ShadowStrength("Shadow Strength", Float) = 1.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                return o;
            }

            sampler2D _MainTex;
            sampler2D _Traced;

			float _InvGamma;
			float _ShadowStrength;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
				float4 light = tex2D(_Traced, i.uv);
                col.rgb *= max(pow(light.rgb / light.a, float3(_InvGamma, _InvGamma, _InvGamma)), float3(1.0 - _ShadowStrength, 1.0 - _ShadowStrength, 1.0 - _ShadowStrength));
                return col;
            }
            ENDCG
        }
    }
}
