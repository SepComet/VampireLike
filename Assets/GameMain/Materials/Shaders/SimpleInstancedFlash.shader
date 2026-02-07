Shader "Custom/SimpleInstancedFlash"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1) // 闪白颜色
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0 // 0=不闪, 1=全闪
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // 开启 GPU Instancing 必须的宏
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 1. 输入结构体必须包含 Instance ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 2. 输出结构体必须包含 Instance ID
            };

            // 3. 定义 Instancing 缓冲区 (这里定义的数据每个物体都可以不同)
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FlashColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlashAmount)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 4. 设置 Instance ID
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(0,0,0,0));
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 5. 片元着色器也要 Setup Instance ID
                UNITY_SETUP_INSTANCE_ID(input);

                // 6. 使用宏读取属性 (代替直接写 _BaseColor)
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                float4 flashColor = UNITY_ACCESS_INSTANCED_PROP(Props, _FlashColor);
                float flashAmount = UNITY_ACCESS_INSTANCED_PROP(Props, _FlashAmount);

                // 简单光照计算 (Lambert)
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                float3 lighting = baseColor.rgb * (mainLight.color * NdotL + unity_AmbientSky.rgb); // 加上环境光防止背光全黑

                // 混合闪白效果
                float3 finalColor = lerp(lighting, flashColor.rgb, flashAmount);

                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
}