Shader "CefOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_OverlayColor("Overlay Color", Color) = (0,1,0,1)
		_Transparency("Transparancy", Range(0.0, 1.0)) = 0.8
    }
    SubShader
    {
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}

			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _OverlayColor;
			float _Transparency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
              fixed4 col = tex2D(_MainTex, i.uv);
				      clip(distance(col, _OverlayColor) > 0.003 ? 1 : -1);
					  col.a = _Transparency;
				      return col;
			      }
            ENDCG
        }
    }
}
