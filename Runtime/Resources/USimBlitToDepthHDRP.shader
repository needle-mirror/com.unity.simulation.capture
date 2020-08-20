Shader "usim/BlitCopyDepthHDRP"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Custom Pass 0"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM

            #pragma multi_compile HDRP_ENABLED
            #pragma only_renderers d3d11 vulkan metal
            #pragma target 4.5

#if HDRP_ENABLED
            #pragma vertex Vert
            #pragma fragment FullScreenPass

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

            float4 FullScreenPass(Varyings varyings) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
                float depth = LoadCameraDepth(varyings.positionCS.xy);
                return float4(depth, depth, depth, 1);
            }
#endif
            ENDHLSL
        }
    }
    Fallback Off
}