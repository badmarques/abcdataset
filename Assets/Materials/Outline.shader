Shader "Hidden/Shader/Outline"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        float3 viewSpaceDir : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
		// Transform our point first from clip to view space,
		// taking the xyz to interpret it as a direction.
		output.viewSpaceDir = mul(UNITY_MATRIX_I_P, output.positionCS).xyz;
        return output;
    }

    // List of properties to control your post process effect
    float _Intensity;
    float _Scale;
    float4 _Color;

    float _DepthThreshold;
    float _DepthNormalThreshold;
	float _DepthNormalThresholdScale;
    float _NormalThreshold;
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

    // Returns the normal vector in view space sampled from the G-buffer
    float3 GetNormalVS(float2 positionSS)
    {
        NormalData normalData;
        DecodeFromNormalBuffer(positionSS, normalData);
        float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalData.normalWS));
        return normalVS; //return float3(normalVS.xy, -normalVS.z);
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

        // Clamp to [0,1) to prevent false outlines at the borders
        bottomLeftUV = clamp(bottomLeftUV, 0, 0.9999999);
        topRightUV = clamp(topRightUV, 0, 0.999999);
        bottomRightUV = clamp(bottomRightUV, 0, 0.9999999);
        topLeftUV = clamp(topLeftUV, 0, 0.9999999);

        float3 normal0 = GetNormalVS(bottomLeftUV * _ScreenSize.xy);
        float3 normal1 = GetNormalVS(topRightUV * _ScreenSize.xy);
        float3 normal2 = GetNormalVS(bottomRightUV * _ScreenSize.xy);
        float3 normal3 = GetNormalVS(topLeftUV * _ScreenSize.xy);

		float depth0 = SampleCameraDepth(bottomLeftUV).r;
		float depth1 = SampleCameraDepth(topRightUV).r;
		float depth2 = SampleCameraDepth(bottomRightUV).r;
		float depth3 = SampleCameraDepth(topLeftUV).r;

	    float NdotV = 1 - dot(normal0, -input.viewSpaceDir);

		// Return a value in the 0...1 range depending on where NdotV lies 
		// between _DepthNormalThreshold and 1.
		float normalThreshold01 = saturate((NdotV - _DepthNormalThreshold) / (1 - _DepthNormalThreshold));
		// Scale the threshold, and add 1 so that it is in the range of 1..._NormalThresholdScale + 1.
		float normalThreshold = normalThreshold01 * _DepthNormalThresholdScale + 1;

		// Modulate the threshold by the existing depth value;
		// pixels further from the screen will require smaller differences
		// to draw an edge.
		float depthThreshold = _DepthThreshold * depth0 * normalThreshold;

		float depthFiniteDifference0 = depth1 - depth0;
		float depthFiniteDifference1 = depth3 - depth2;
		// edgeDepth is calculated using the Roberts cross operator.
		// The same operation is applied to the normal below.
		// https://en.wikipedia.org/wiki/Roberts_cross
		float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
		edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

        float3 normalFiniteDifference0 = normal1 - normal0;
        float3 normalFiniteDifference1 = normal3 - normal2;
        // Dot the finite differences with themselves to transform the 
		// three-dimensional values to scalars.
        float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
        edgeNormal = edgeNormal > _NormalThreshold ? 1 : 0;

		float edge = max(edgeDepth, edgeNormal);

		float4 edgeColor = float4(_Color.rgb, _Color.a * edge);        
		//float4 color = LOAD_TEXTURE2D_X(_InputTexture, positionSS);

        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        return float4(edge.rrr, 1);
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Outline"
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