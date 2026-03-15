Shader "UI/AlphaMask"
{
    // 마스크 텍스처의 알파(또는 R 채널)로 메인 텍스처를 클리핑하는 단일 머티리얼 셰이더.
    // 스텐실 없이 한 장의 머티리얼로 마스킹 처리 가능.
    //
    // [사용법]
    // 1. 이 셰이더로 머티리얼 하나 생성
    // 2. _MainTex: 보여줄 이미지
    // 3. _MaskTex: 마스크 형태 텍스처 (흰=표시, 검=숨김, 알파 채널 사용)
    // 4. _MaskChannel: 마스크로 사용할 채널 (0=Alpha, 1=Red, 2=Green, 3=Blue)
    // 5. Image 컴포넌트에 머티리얼 적용

    Properties
    {
        _MainTex    ("Main Texture",  2D)    = "white" {}
        _MaskTex    ("Mask Texture",  2D)    = "white" {}
        _Color      ("Tint",          Color) = (1, 1, 1, 1)
        [KeywordEnum(Alpha, Red, Green, Blue)]
        _MaskChannel ("Mask Channel", Float) = 0
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
            Blend  SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull   Off

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _MASKCHANNEL_ALPHA _MASKCHANNEL_RED _MASKCHANNEL_GREEN _MASKCHANNEL_BLUE
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _MaskTex;
            float4    _MaskTex_ST;
            fixed4    _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos     : SV_POSITION;
                float2 uvMain  : TEXCOORD0;
                float2 uvMask  : TEXCOORD1;
                fixed4 color   : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.uvMain = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvMask = TRANSFORM_TEX(v.uv, _MaskTex);
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col  = tex2D(_MainTex, i.uvMain) * i.color;
                fixed4 mask = tex2D(_MaskTex, i.uvMask);

                #if defined(_MASKCHANNEL_RED)
                    col.a *= mask.r;
                #elif defined(_MASKCHANNEL_GREEN)
                    col.a *= mask.g;
                #elif defined(_MASKCHANNEL_BLUE)
                    col.a *= mask.b;
                #else // _MASKCHANNEL_ALPHA (default)
                    col.a *= mask.a;
                #endif

                return col;
            }
            ENDCG
        }
    }
}
