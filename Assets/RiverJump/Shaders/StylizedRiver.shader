Shader "RiverJump/StylizedRiver"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.12, 0.78, 0.92, 1)
        _DeepColor("Deep Color", Color) = (0.02, 0.28, 0.68, 1)
        _FoamColor("Foam Color", Color) = (0.75, 0.98, 1, 1)
        _WaveScale("Wave Scale", Float) = 1.5
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveHeight("Wave Height", Float) = 0.08
        _Smoothness("Smoothness", Range(0, 1)) = 0.72
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _WaveScale;
                float _WaveSpeed;
                float _WaveHeight;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float wave : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float waveA = sin(world.x * _WaveScale + _Time.y * _WaveSpeed);
                float waveB = cos(world.z * (_WaveScale * 0.73) - _Time.y * (_WaveSpeed * 0.82));
                float wave = (waveA + waveB) * 0.5;
                world.y += wave * _WaveHeight;
                output.positionWS = world;
                output.positionCS = TransformWorldToHClip(world);
                output.normalWS = normalize(float3(-waveA * 0.08, 1, -waveB * 0.06));
                output.wave = wave;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float depthPattern = saturate(0.5 + sin(input.positionWS.z * 0.22) * 0.16 + input.wave * 0.18);
                half3 baseColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, depthPattern);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float diffuse = saturate(dot(normalize(input.normalWS), mainLight.direction)) * mainLight.shadowAttenuation;
                float foamLine = smoothstep(0.82, 0.96, sin(input.positionWS.x * 1.35 + input.positionWS.z * 0.55 + _Time.y * 1.8) * 0.5 + 0.5);
                foamLine *= smoothstep(0.35, 0.75, input.wave * 0.5 + 0.5);
                half3 color = baseColor * (0.7 + diffuse * 0.38) + mainLight.color * diffuse * 0.08;
                color = lerp(color, _FoamColor.rgb, foamLine * 0.48);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
