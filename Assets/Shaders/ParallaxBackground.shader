Shader "Game/Parallax Background"
{
    Properties
    {
        _MainTex ("Background", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _UVTransform ("UV Scale and Offset", Vector) = (1,1,0,0)
        _SourceRect ("Source Rect", Vector) = (0,0,1,1)
        _HorizontalFill ("Horizontal Patch Coverage", Range(0.001, 1)) = 1
        _WorldMinX ("World Left Boundary", Float) = -100000
        _WorldMaxX ("World Right Boundary", Float) = 100000
        _WorldBottomY ("World Bottom Boundary", Float) = -100000
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "Background"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float4 _Tint;
                float4 _UVTransform;
                float4 _SourceRect;
                float _WorldBottomY;
                float _WorldMinX;
                float _WorldMaxX;
                float _HorizontalFill;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 worldXY : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.worldXY = TransformObjectToWorld(input.positionOS.xyz).xy;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                clip(input.worldXY.y - _WorldBottomY);
                clip(input.worldXY.x - _WorldMinX);
                clip(_WorldMaxX - input.worldXY.x);
                float2 uv = input.uv * _UVTransform.xy + _UVTransform.zw;
                // 草地可留出间距；纹理完整映射到每片草地内部，不拉伸素材填满空隙。
                float fill = clamp(_HorizontalFill, 0.001, 1);
                float margin = (1 - fill) * 0.5;
                float cell = floor(uv.x);
                float phase = frac(uv.x);
                clip(phase - margin);
                clip(1 - margin - phase);
                uv.x = cell + saturate((phase - margin) / fill);
                // 在每个拼接处采样同一边缘：普通素材也不会出现首尾颜色跳变。
                uv.x = 1 - abs(frac(uv.x * 0.5) * 2 - 1);
                uv.y = saturate(uv.y);
                float2 inset = _MainTex_TexelSize.xy * 0.5;
                float2 first = _SourceRect.xy + inset;
                float2 last = _SourceRect.xy + _SourceRect.zw - inset;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, lerp(first, last, uv)) * _Tint;
            }
            ENDHLSL
        }
    }
}
