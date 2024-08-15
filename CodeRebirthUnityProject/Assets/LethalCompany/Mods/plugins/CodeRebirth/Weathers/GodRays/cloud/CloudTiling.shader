// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader"Unlit/CloudTiling"
{
    Properties
    {
        _offsetX ("OffsetX",Float) = 0.0
        _offsetY ("OffsetY",Float) = 0.0       
        _octaves ("Octaves",Int) = 7
        _lacunarity("Lacunarity", Range( 1.0 , 5.0)) = 2
        _gain("Gain", Range( 0.0 , 1.0)) = 0.5
        _value("Value", Range( -2.0 , 2.0)) = 0.0
        _amplitude("Amplitude", Range( 0.0 , 5.0)) = 1.5
        _frequency("Frequency", Range( 0.0 , 6.0)) = 2.0
        _power("Power", Range( 0.1 , 5.0)) = 1.0
        _scale("Scale", Float) = 1.0
        _color ("Color", Color) = (1.0,1.0,1.0,1.0)       
        [Toggle] _monochromatic("Monochromatic", Float) = 0
        _range("Monochromatic Range", Range( 0.0 , 1.0)) = 0.5   
        _renderOriginX("Render Origin X", Float) = 0.0
        _renderOriginY("Render Origin Y", Float) = 0.0
        _renderDistance("Render Distance", Float) = 1.0
        _renderDistanceFalloff("Render Distance Falloff", Float) = 0.0
        _renderDistanceLmiitColour("Render Distance Limit Colour", Color) = (0.5,0.5,0.5,1.0)
        _skyBoxRadius("Sky Box Radius", Float) = 100.0
    }
    Subshader
    {
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vertex_shader
            #pragma fragment pixel_shader
            #pragma target 3.0
           
            struct SHADERDATA
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldSpacePosition : TEXCOORD1;
            };

            struct GodRay
            {
                float4 colour;
                float3 topPosition;
                float3 bottomPosition;
                float radius;
                float falloff;
            };

            float _octaves, _lacunarity, _gain, _value, _amplitude, _frequency, _offsetX, _offsetY, _power, _scale, _monochromatic, _range;
            float4 _color;
            StructuredBuffer<GodRay> _rays;
            int _rayCount;
            float _renderDistance, _renderDistanceFalloff;
            float4 _renderDistanceLmiitColour;
            float _renderOriginX, _renderOriginY;
            float _skyBoxRadius;
           
            float fbm(float2 p)
            {
                p = p * _scale + float2(_offsetX, _offsetY);
                for (int i = 0; i < _octaves; i++)
                {
                    float2 i = floor(p * _frequency);
                    float2 f = frac(p * _frequency);
                    float2 t = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                    float2 a = i + float2(0.0, 0.0);
                    float2 b = i + float2(1.0, 0.0);
                    float2 c = i + float2(0.0, 1.0);
                    float2 d = i + float2(1.0, 1.0);
                    a = -1.0 + 2.0 * frac(sin(float2(dot(a, float2(127.1, 311.7)), dot(a, float2(269.5, 183.3)))) * 43758.5453123);
                    b = -1.0 + 2.0 * frac(sin(float2(dot(b, float2(127.1, 311.7)), dot(b, float2(269.5, 183.3)))) * 43758.5453123);
                    c = -1.0 + 2.0 * frac(sin(float2(dot(c, float2(127.1, 311.7)), dot(c, float2(269.5, 183.3)))) * 43758.5453123);
                    d = -1.0 + 2.0 * frac(sin(float2(dot(d, float2(127.1, 311.7)), dot(d, float2(269.5, 183.3)))) * 43758.5453123);
                    float A = dot(a, f - float2(0.0, 0.0));
                    float B = dot(b, f - float2(1.0, 0.0));
                    float C = dot(c, f - float2(0.0, 1.0));
                    float D = dot(d, f - float2(1.0, 1.0));
                    float noise = (lerp(lerp(A, B, t.x), lerp(C, D, t.x), t.y));
                    _value += _amplitude * noise;
                    _frequency *= _lacunarity;
                    _amplitude *= _gain;
                }
                _value = clamp(_value, -1.0, 1.0);
                return pow(_value * 0.5 + 0.5, _power);
            }

            float fbm_standard(float2 p)
            {
                p = p;
                for (int i = 0; i < _octaves; i++)
                {
                    float2 i = floor(p * _frequency);
                    float2 f = frac(p * _frequency);
                    float2 t = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                    float2 a = i + float2(0.0, 0.0);
                    float2 b = i + float2(1.0, 0.0);
                    float2 c = i + float2(0.0, 1.0);
                    float2 d = i + float2(1.0, 1.0);
                    a = -1.0 + 2.0 * frac(sin(float2(dot(a, float2(127.1, 311.7)), dot(a, float2(269.5, 183.3)))) * 43758.5453123);
                    b = -1.0 + 2.0 * frac(sin(float2(dot(b, float2(127.1, 311.7)), dot(b, float2(269.5, 183.3)))) * 43758.5453123);
                    c = -1.0 + 2.0 * frac(sin(float2(dot(c, float2(127.1, 311.7)), dot(c, float2(269.5, 183.3)))) * 43758.5453123);
                    d = -1.0 + 2.0 * frac(sin(float2(dot(d, float2(127.1, 311.7)), dot(d, float2(269.5, 183.3)))) * 43758.5453123);
                    float A = dot(a, f - float2(0.0, 0.0));
                    float B = dot(b, f - float2(1.0, 0.0));
                    float C = dot(c, f - float2(0.0, 1.0));
                    float D = dot(d, f - float2(1.0, 1.0));
                    float noise = (lerp(lerp(A, B, t.x), lerp(C, D, t.x), t.y));
                    _value += _amplitude * noise;
                    _frequency *= _lacunarity;
                    _amplitude *= _gain;
                }
                _value = clamp(_value, -1.0, 1.0);
                return pow(_value * 0.5 + 0.5, _power);
            }
           
            SHADERDATA vertex_shader(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                SHADERDATA vs;
                vs.vertex = UnityObjectToClipPos(vertex);
                vs.worldSpacePosition = mul(unity_ObjectToWorld, vertex);
                vs.uv = uv;
                return vs;
            }

            float smoothstep(float t, float a, float b)
            {
                t = saturate((t - a) / (b - a));
                return t * t * (3 - 2 * t);

            }

            float2 getUV(float3 coords, float radius)
            {
                float3 dx = coords;
                dx /= radius;
                float b = dx.y;
                float a = length(dx.xz);
                float d = a/b;
                return dx.xz / abs(b);
            }

            float2 getGodRayUV(float3 topCoords, float3 bottomCoords, float radius)
            {
                
                float3 bottom = float3(bottomCoords.x-_WorldSpaceCameraPos.x, bottomCoords.y, bottomCoords.z-_WorldSpaceCameraPos.z)/radius;
                float3 top = float3(topCoords.x-_WorldSpaceCameraPos.x, topCoords.y, topCoords.z-_WorldSpaceCameraPos.z)/radius;
                float3 d = top-bottom;

                float a = dot(d, d);
                float b = 2 * dot(bottom, d);
                float c = dot(bottom, bottom) - 1.0;

                float discriminantSqrt = sqrt(b*b - 4*a*c);
                // is it correct to ignore the negative sqrt?
                float lambda = (-b + discriminantSqrt)/(2*a);

                float3 p = bottom + d*lambda;
                // do we need to divide by the y coordinate?
                return p.xz;

            }

            float4 pixel_shader(SHADERDATA ps) : SV_TARGET
            {
                float2 uv = getUV(float3(ps.worldSpacePosition.x, ps.worldSpacePosition.y-_WorldSpaceCameraPos.y, ps.worldSpacePosition.z), _skyBoxRadius);
                // float2 godRayUV = getGodRayUV(ps.worldSpacePosition.xyz, float3(ps.worldSpacePosition.x, ps.worldSpacePosition.y-1, ps.worldSpacePosition.z), _skyBoxRadius);
                float2 godRayUV = (ps.worldSpacePosition.xz-_WorldSpaceCameraPos.xz)/_skyBoxRadius;
                

                float distance = length(uv-float2(_renderOriginX, _renderOriginY));
                float fogInfluence = smoothstep((ps.worldSpacePosition.y-_WorldSpaceCameraPos.y)/_skyBoxRadius, _renderDistance+_renderDistanceFalloff, _renderDistance);
                float c = fbm(uv);
                float3 colour = lerp(float3(0, 0, 0), _color.rgb, c);
                for (int i = 0; i < _rayCount; i++)
                {
                    float2 godRay = getGodRayUV(_rays[i].topPosition, _rays[i].bottomPosition, _skyBoxRadius);

                    float radiusMultiplier = 1;
                    colour = lerp(colour, _rays[i].colour, smoothstep(length(godRayUV - godRay), (_rays[i].radius * radiusMultiplier + _rays[i].falloff)/_skyBoxRadius, (_rays[i].radius * radiusMultiplier)/_skyBoxRadius));
                }
                return lerp(float4(colour, 1), _renderDistanceLmiitColour*_color, fogInfluence);
            }

            ENDCG

        }
    }
}