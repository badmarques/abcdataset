Shader "Hidden/Shader/GrayScale"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // List of properties to control your post process effect
    float _Intensity;
    float _Scale;
    float4 _Color;

    float _DepthThreshold;
    TEXTURE2D_X(_InputTexture);

	// Combines the top and bottom colors using normal blending.
	// https://en.wikipedia.org/wiki/Blend_modes#Normal_blend_mode
	// This performs the same operation as Blend SrcAlpha OneMinusSrcAlpha.
	float4 alphaBlend(float4 top, float4 bottom)
	{
		float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
		float alpha = top.a + bottom.a * (1 - top.a);

		return float4(color, alpha);
	}

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        float2 TexelSize = float2(1.0 / _ScreenSize.xy);

        uint2 positionSS = input.texcoord * _ScreenSize.xy;
		float halfScaleFloor = floor(_Scale * 0.5);
		float halfScaleCeil = ceil(_Scale * 0.5);

		// Sample the pixels in an X shape, roughly centered around i.texcoord.
		// As the _CameraDepthTexture and _CameraNormalsTexture default samplers
		// use point filtering, we use the above variables to ensure we offset
		// exactly one pixel at a time.
		float2 bottomLeftUV = input.texcoord - float2(TexelSize.x, TexelSize.y) * halfScaleFloor;
		float2 topRightUV = input.texcoord + float2(TexelSize.x, TexelSize.y) * halfScaleCeil;  
		float2 bottomRightUV = input.texcoord + float2(TexelSize.x * halfScaleCeil, -TexelSize.y * halfScaleFloor);
		float2 topLeftUV = input.texcoord + float2(-TexelSize.x * halfScaleFloor, TexelSize.y * halfScaleCeil);

		float depth0 = SampleCameraDepth(bottomLeftUV).r;
		float depth1 = SampleCameraDepth(topRightUV).r;
		float depth2 = SampleCameraDepth(bottomRightUV).r;
		float depth3 = SampleCameraDepth(topLeftUV).r;

		// Modulate the threshold by the existing depth value;
		// pixels further from the screen will require smaller differences
		// to draw an edge.
		float depthThreshold = _DepthThreshold * depth0;// * normalThreshold;

		float depthFiniteDifference0 = depth1 - depth0;
		float depthFiniteDifference1 = depth3 - depth2;
		// edgeDepth is calculated using the Roberts cross operator.
		// The same operation is applied to the normal below.
		// https://en.wikipedia.org/wiki/Roberts_cross
		float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
		edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

		float edge = edgeDepth; //max(edgeDepth, edgeNormal);

		//float4 edgeColor = float4(_Color.rgb, _Color.a * edge);        
		//float4 color = LOAD_TEXTURE2D_X(_InputTexture, positionSS);

        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        //return float4(lerp(outColor, Luminance(outColor).xxx, _Intensity), 1);
        
        //float3 outColor = alphaBlend(edgeColor, color);
        //return float4(outColor, 1);
        return float4(edge.xxx, 1);

        //return float4(edge.xxx, 1);
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "GrayScale"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}