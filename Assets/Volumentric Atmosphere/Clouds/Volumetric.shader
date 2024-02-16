Shader "Unlit/Volumetric 1"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

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
                float3 viewVector : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(v.vertex);
                output.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(output,output.vertex);
                float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                output.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));
                return output;
            }

            float3 boundsMin;
            float3 boundsMax;
            Texture3D cloudNoise;
            SamplerState samplercloudNoise;
            float scale;
            float4 offset;
            int sampleCount;
            float lightReflectionScale;
            float lightTransmittanceScale;
            float colorTransmittanceScale;
            float densityMinimumThreshold;

            // Returns (distanceToBox, distanceInsideBox)
            float2 rayBoxDistance(float3 rayOrigin, float3 rayVector) {
                float3 temp0 = (boundsMin - rayOrigin) * rayVector;
                float3 temp1 = (boundsMax - rayOrigin) * rayVector;
                float3 tempMin = min(temp0, temp1);
                float3 tempMax = max(temp0, temp1);

                float distanceA = max(max(tempMin.x, tempMin.y), tempMin.z);
                float distanceB = min(tempMax.x, min(tempMax.y, tempMax.z));

                // CASE 1: ray intersects box from outside (0 <= distanceA <= distanceB)
                // distanceA is distance to nearest intersection, distanceB distance to far intersection

                // CASE 2: ray intersects box from inside (distanceA < 0 < distanceB)
                // distanceA is the distance to intersection behind the ray, distanceB is distance to forward intersection

                // CASE 3: ray misses box (distanceA > distanceB)

                float distanceToBox = max(0, distanceA);
                float distanceInsideBox = max(0, distanceB - distanceToBox);
                
                /*
                CASE 1: Ray is ins ide box
                distanceInsideBox == 0 && distanceToBox > 0
                
                CASE 2: Ray intersects box:
                0 < distanceToBox < distanceInsideBox

                CASE 3: Ray misses box
                distanceToBox > distanceInsideBox
                */
                return float2(distanceToBox, distanceInsideBox);
            }

            float sampleCloudDensity (float3 position) {
                float3 offsetSamplePosition = position*scale + offset.xyz;
                // Convert to texture coordinates
                // Assume texture is 1,1,1 size at zero position, so wrap around such size
                float3 textureSamplePosition = abs(fmod(offsetSamplePosition, 1));
                // Find color of pixel[]
                float density = cloudNoise.SampleLevel(samplercloudNoise, textureSamplePosition, 0);

                // Check minimum density
                if (density > densityMinimumThreshold) return density;
                else return 0;
            }

            float lightmarch(float3 position) {
                /*
                Sample from position to the direction of the light source
                sample <sampleIntervalDistance> number of times
                Return the proportion of light to be reflected
                */

                // Find the intersecting point between position and light
                float3 lightVector = normalize(_WorldSpaceLightPos0.xyz);
                float distanceToIntersection = rayBoxDistance(position, -1/lightVector).y;
                //if (distanceToIntersection < 0.01) return 1;

                // Sample points along distance
                float density = 0;
                float3 samplePosition = position;
                float stepDistance = distanceToIntersection / sampleCount;
                float3 stepDirection = -lightVector * stepDistance;

                for (int x = 0; x < sampleCount; x++) {
                    density += sampleCloudDensity(samplePosition) * stepDistance;
                    
                    // Update next iteration
                    samplePosition += stepDirection;
                }
                return (exp(density * lightTransmittanceScale)-1) * sampleCloudDensity(position) * lightReflectionScale;
            }
            

            float4 frag (v2f input) : SV_Target
            {
                float4 color = tex2D(_MainTex, input.uv);
                float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv);
                float viewDepth = LinearEyeDepth(nonLinearDepth) * length(input.viewVector);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayVector = normalize(input.viewVector);
                
                // Check if ray intersects with bounds of box
                float2 rayToContainerInfo = rayBoxDistance(rayOrigin, 1/rayVector);
                float distanceToContainer = rayToContainerInfo.x;
                float distanceInsideContainer = rayToContainerInfo.y;
                
                // If ray is within container
                if (distanceInsideContainer > 0 && distanceToContainer < viewDepth) {
                    // Account for objects within container
                    if (distanceToContainer + distanceInsideContainer > viewDepth) {
                        distanceInsideContainer = viewDepth - distanceToContainer;
                    }

                    // Find location of intersection 
                    float3 intersectionLocation = rayOrigin + rayVector * distanceToContainer;
                    float3 samplePosition = intersectionLocation;
                    // Sample till <sampleMaxDistance>
                    float3 stepVector = rayVector * distanceInsideContainer / sampleCount;
                    float stepDistance = length(stepVector);
                    
                    float totalCloudDensity = 0;
                    float totalLight = 0;

                    // Sample points along ray
                    for (int x = 0; x <= sampleCount; x++) {
                        totalCloudDensity += sampleCloudDensity(samplePosition) * stepDistance;
                        totalLight += lightmarch(samplePosition);

                        // Update for next iteration
                        samplePosition += stepVector;
                    }
                    float4 finalColor = exp(-totalCloudDensity * colorTransmittanceScale) * color;
                    finalColor += totalLight;

                    
                    return finalColor;
                }
                // If not intersecting with container
                else return color;
            }
            ENDCG
        }
    }
}




















                    // float stepDistance = distanceInsideContainer/sampleIntervalDistance;
                    // float3 stepDirection = rayVector * stepDistance;

                    // float totalDensity = 0;
                    // float totalLightTransmittance = 0;

                    // // Sample points along the ray
                    // for (int x = 0; x < sampleIntervalDistance; x++) {
                    //     /*
                    //     For each point, 
                    //     Find the density of each point
                    //     find out how much light has reached that point
                    //     */
                    //     // Add light to point
                    //     totalLightTransmittance += lightmarch(samplePosition) * exp(-stepDistance * x);
                        
                    //     // Add density of point
                    //     totalDensity += sampleCloudDensity(samplePosition) * stepDistance;

                    //     // Update next iteration of samplePosition
                    //     samplePosition += stepDirection;
                    // }

                    // float4 finalColor = exp(-totalDensity * colorTransmittanceScale) * color;
                    // finalColor += totalLightTransmittance * lightReflectionScale * _LightColor0;
                    // return finalColor;
