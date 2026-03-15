Shader "UI/StencilMasked"
{
    // 스텐실 버퍼 값이 1인 픽셀에서만 렌더링하는 셰이더.
    // UIStencilMask 셰이더로 정의된 마스크 영역 안에서만 이미지가 보인다.
    //
    // [사용법]
    // 1. UIStencilMask 머티리얼을 마스크 역할 Image에 먼저 적용 (렌더 순서 앞)
    // 2. 이 셰이더로 머티리얼 생성 → 클리핑될 Image에 적용

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint",    Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            // 스텐실 값이 1인 픽셀에서만 통과
            Stencil
            {
                Ref  1
                Comp Equal
                Pass Keep
            }

            Blend  SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull   Off

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                return col;
            }
            ENDCG
        }
    }
}
