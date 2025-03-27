Shader "LineShader"
{
    SubShader
    {
        // Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        // Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            // Upgrade NOTE : excluded shader from DX11; has structs without semantics (struct v2f members depth)
            #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
                float4 wpos : COLOR1;
                float4 tangent : NORMAL;
                float depth : DEPTH;
                float psize : PSIZE;
            };

            StructuredBuffer<int> uIndices;
            StructuredBuffer<float> uSignalBuffer;
            uniform uint uStartIndex;
            uniform uint uBaseVertexIndex;
            uniform float uLag1;
            uniform float uLag2;
            uniform uint uNumVerts;
            uniform float uMinDepth;
            uniform float uMaxDepth;
            uniform float uMinIntensity;
            uniform float uMaxIntensity;

            uniform float kAmbient;
            uniform float kDiffuse;
            uniform float kSpecular;
            uniform float nSpecular;
            uniform float4 colorAmbient;
            uniform float4 colorDiffuse;
            uniform float4 colorSpecular;

            uniform float4x4 uObjectToWorld;
            uniform float uNumInstances;

            float interpSignal(float index){
                float lowerVal = uSignalBuffer[floor(index) % uNumVerts];
                float upperVal = uSignalBuffer[ceil(index) % uNumVerts];
                float fract = frac(index);

                if (ceil(index) >= uNumVerts){
                    return 0.0f;
                }
                return lerp(lowerVal, upperVal, fract);
            }

            v2f vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                v2f o;
                uint actualID = uIndices[vertexID + uStartIndex] + uBaseVertexIndex;
                float normalizedID = float(actualID) / float(uNumVerts);

                float3 pos;
                pos.x = uSignalBuffer[actualID];
                pos.y = interpSignal((float(actualID) + uLag1));
                pos.z = interpSignal((float(actualID) + uLag2));

                float3 prevPos;
                prevPos.x = uSignalBuffer[actualID-1];
                prevPos.y = interpSignal((float(actualID-1) + uLag1));
                prevPos.z = interpSignal((float(actualID-1) + uLag2));
                
                float3 nextPos;
                nextPos.x = uSignalBuffer[actualID+1];
                nextPos.y = interpSignal((float(actualID+1) + uLag1));
                nextPos.z = interpSignal((float(actualID+1) + uLag2));
                
                float3 t1 = pos-prevPos;
                float3 t2 = nextPos-pos;
                float3 T = normalize(t1 + t2);
                o.tangent = normalize(mul(uObjectToWorld, float4(T, 0.0f)));
                
                float4 wpos = mul(uObjectToWorld, float4(pos, 1.0f));
                o.wpos = wpos;
                o.pos = UnityObjectToClipPos(wpos);
                
                o.color = float4(1.0, 1.0, 1.0, 1.0);
                o.depth = - UnityObjectToViewPos(wpos).z;
                o.psize = 5.0f/o.depth;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 L = float3(0.0, 0.0, 1.0);
                float LdotT = dot(L, i.tangent.xyz);
                float VdotT = dot(normalize(i.wpos.xyz), i.tangent.xyz);
                float VdotR = sqrt(saturate(1-LdotT*LdotT))*sqrt(saturate(1-VdotT*VdotT));
                float LdotN = sqrt(saturate(1-LdotT*LdotT));

                float4 ambient = kAmbient*colorAmbient;
                float4 diffuse = kDiffuse*LdotN*colorDiffuse;
                float4 specular = kSpecular*pow(VdotR, nSpecular)*colorSpecular;

                return ambient + diffuse + specular;
            }
            ENDCG
        }
    }
}