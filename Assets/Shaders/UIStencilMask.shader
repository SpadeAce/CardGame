Shader "UI/StencilMask"
{
    // 마스크 영역을 스텐실 버퍼에 기록하고 화면에는 그리지 않는 셰이더.
    // Image 컴포넌트에 이 머티리얼을 적용하면 해당 Image의 형태(알파 포함)가
    // 마스크 영역으로 등록된다.
    //
    // [사용법]
    // 1. 이 셰이더로 머티리얼 생성 → 마스크 역할 Image에 적용
    // 2. UIStencilMasked 머티리얼을 클리핑할 Image에 적용
    // 3. Hierarchy에서 마스크 오브젝트가 Masked 오브젝트보다 위(먼저 렌더)에 있어야 함

    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
    }

    SubShader
    {
        // Transparent-1: UIStencilMasked(Transparent)보다 먼저 렌더되어 스텐실 버퍼를 준비
        Tags
        {
            "Queue"           = "Transparent-1"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            // 스텐실 버퍼에 1을 기록
            Stencil
            {
                Ref  1
                Comp Always
                Pass Replace
            }

            // 컬러 버퍼와 깊이 버퍼에는 쓰지 않음 → 화면에 보이지 않음
            ColorMask 0
            ZWrite    Off
            Cull      Off

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // 텍스처 투명 영역은 마스크에서 제외
                clip(col.a - 0.01);
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
