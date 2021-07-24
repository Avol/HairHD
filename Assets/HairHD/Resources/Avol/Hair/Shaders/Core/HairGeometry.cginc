// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

struct HairPointData
{
    float3 Position;
    float3 PrevPosition;
    float3 RotationAngles;
    float3 Color;
 
    int StrandID;
    int StrandPoint;
    float StrandThickness;
    float StrandStiffness;
    float Retenton;
};

struct HairStrandData
{
    float3 Tangent;
    float3 Bitangent;
    float SegmentLength;
};


StructuredBuffer<HairPointData>     _HairPointData;
StructuredBuffer<HairStrandData>    _HairStrandData;

float4x4 _CameraPV;

uniform int _StrandSegments;
uniform int _Cascade;
uniform int _CascadeCount;
uniform int _LightIndex;
uniform float _TextureWidth;
uniform float _TextureHeight;
uniform int _Orthographic;


struct input
{
    float4  position        : SV_POSITION;
    float4  nextPosition    : COLOR1;
    float4  data            : COLOR0;
    float3  tanget          : NORMAL;
    float2  uv              : TEXCOORD0;
    float2  screenSize      : TEXCOORD1;
    float4  color           : COLOR2;
};

float4 WorldToClipSpace(float3 vertex)
{
    float4 pos = mul(_CameraPV, float4(vertex, 1));
    pos.y *= -1;
    return pos;
}

float4 MainLightShadowTransform(float3 worldPos)
{
    int cascadeIndex = ComputeCascadeIndex(worldPos);
    float4 shadowCoord = 0;
    
    shadowCoord = mul(_MainLightWorldToShadow[_Cascade], float4(worldPos, 1.0));
    shadowCoord.y = 1.0f - shadowCoord.y;
    shadowCoord.xy = shadowCoord.xy * 2.0f - 1.0f;

    return shadowCoord;
}

float4 AdditionalLightShadowTransform(int lightIndex,  float3 worldPos)
{
    float4 shadowCoord = 0;

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    shadowCoord = mul(_AdditionalLightsWorldToShadow_SSBO[lightIndex], float4(worldPos, 1.0f));
#else
    shadowCoord = mul(_AdditionalLightsWorldToShadow[lightIndex], float4(worldPos, 1.0f));
#endif

    shadowCoord.xy /= shadowCoord.w;

    shadowCoord.y = 1.0f - shadowCoord.y;
    shadowCoord.xy = shadowCoord.xy * 2.0f - 1.0f;

    shadowCoord.xy *= shadowCoord.w;

    return shadowCoord;
}

// computes quad points in screen space of the projector.
// TODO: still bad.
void ComputeQuadPoints(uint id, float4 p1, float4 p2, 
                    out float4 o1, out float4 o2, out float4 o3, out float4 o4, float t0, float t1)
{
    // ---- Perspective.
    if (_Orthographic == 0)
    {
        float3 ratio = float3(_TextureWidth, _TextureHeight, 0);
        ratio = normalize(ratio);

        float3 _p0 = p1.xyz / p1.w;
        float3 _p1 = p2.xyz / p2.w;

        float3 line01 = _p1 - _p0;
        float3 dir = normalize(line01);
        float3 unit_z = normalize(float3(0, 0, -1));

        float3 normal = normalize(cross(unit_z, dir) * ratio);

        float3 dir_offset_0 = dir * ratio * t0;
        float3 normal_scaled_0 = normal * ratio * t0;

        float3 dir_offset_1 = dir * ratio * t1;
        float3 normal_scaled_1 = normal * ratio * t1;

        float3 p0_ex = _p0 - dir_offset_0;
        float3 p1_ex = _p1 + dir_offset_1;

        o1 = float4(p0_ex - normal_scaled_0 / p1.w, 1) * p1.w;
        o2 = float4(p0_ex + normal_scaled_0 / p1.w, 1) * p1.w;
        o3 = float4(p1_ex + normal_scaled_1 / p2.w, 1) * p2.w;
        o4 = float4(p1_ex - normal_scaled_1 / p2.w, 1) * p2.w;
    }
    
    // ---- Ortographic.
    else
    {
        float2 dir      = normalize(p2.xy / p1.w - p1.xy / p1.w);
        float2 normal   = float2(-dir.y, dir.x);

        float4  offset1  = float4((normal * t0), 0, 0);
        float4  offset2  = float4((normal * t1), 0, 0);

        offset1.x *= _TextureWidth;
        offset2.x *= _TextureWidth;

        offset1.y *= _TextureHeight;
        offset2.y *= _TextureHeight;

        o1 = p1 + offset1;
        o2 = p1 - offset1;

        o3 = p2 + offset2;
        o4 = p2 - offset2;
    }
}

input vert(uint id : SV_VertexID)
{
    input o;
    
    HairPointData pointData = _HairPointData[id];
    
    o.position      = WorldToClipSpace(pointData.Position);


    uint idOffset = 1;
    uint last = pointData.StrandPoint + 1 == _StrandSegments ? 1 : 0;
    idOffset -= last;
    
   
    o.nextPosition  = WorldToClipSpace(_HairPointData[id + idOffset].Position);
    
    // NOTE: position can't be the same. position needs some bias.
    float3 offset = float3(0.001f, 0.001f, 0.001f);
    if (idOffset == 0)
        o.nextPosition = WorldToClipSpace(_HairPointData[id + idOffset].Position + offset);

    float distance = (float) (pointData.StrandPoint + 1) / (float) _StrandSegments;
     
    o.data          = float4(distance, id, pointData.StrandThickness, last);
    
    
    if (pointData.StrandPoint + 3 > _StrandSegments)        o.tanget        = normalize(pointData.Position - _HairPointData[id - 2].Position);
    else                                                    o.tanget        = normalize(_HairPointData[id + 2].Position - pointData.Position);

    o.uv            = 0;
    o.screenSize    = 0;
    o.color         = float4(pointData.Color, 1);

    return o;
}

input vertInternal(uint id : SV_VertexID)
{
    input o;


//GetVertexPositionInputs(v.vertex).positionCS;

    HairPointData pointData = _HairPointData[id];

    o.position = GetVertexPositionInputs(pointData.Position).positionCS;


    uint idOffset = 1;
    uint last = pointData.StrandPoint + 1 == _StrandSegments ? 1 : 0;
    idOffset -= last;

    // _ScreenParams.x

    o.nextPosition = GetVertexPositionInputs(_HairPointData[id + idOffset].Position).positionCS;

    // TODO: fix this properly.
    // can't be the same position needs some bias.
    float3 offset = float3(0.001f, 0.001f, 0.001f);
    if (idOffset == 0)
        o.nextPosition = GetVertexPositionInputs(_HairPointData[id + idOffset].Position + offset).positionCS;

    float distance = (float)(pointData.StrandPoint + 1) / (float)_StrandSegments;

    o.data = float4(distance, id, pointData.StrandThickness, last);


    if (pointData.StrandPoint + 3 > _StrandSegments)        o.tanget = normalize(pointData.Position - _HairPointData[id - 2].Position);
    else                                                    o.tanget = normalize(_HairPointData[id + 2].Position - pointData.Position);

    o.uv            = 0;
    o.screenSize    = 0;
    o.color         = float4(pointData.Color, 1);

    return o;
}

input vertShadowDir(uint id : SV_VertexID)
{
    input o;

    HairPointData pointData = _HairPointData[id];

    o.position = MainLightShadowTransform(pointData.Position);


    uint idOffset = 1;
    uint last = pointData.StrandPoint + 1 == _StrandSegments ? 1 : 0;
    idOffset -= last;


    o.nextPosition = MainLightShadowTransform(_HairPointData[id + idOffset].Position);

    // TODO: fix this properly.
    // can't be the same position needs some bias.
    float3 offset = float3(0.001f, 0.001f, 0.001f);
    if (idOffset == 0)
        o.nextPosition = MainLightShadowTransform(_HairPointData[id + idOffset].Position + offset);

    float distance = (float)(pointData.StrandPoint + 1) / (float)_StrandSegments;

    o.data = float4(distance, id, pointData.StrandThickness, last);


    if (pointData.StrandPoint + 3 > _StrandSegments)        o.tanget = normalize(pointData.Position - _HairPointData[id - 2].Position);
    else                                                    o.tanget = normalize(_HairPointData[id + 2].Position - pointData.Position);

    o.uv = 0;
    o.screenSize = 0;
    o.color = float4(pointData.Color, 1);

    return o;
}

input vertShadowSpot(uint id : SV_VertexID)
{
    input o;

    HairPointData pointData = _HairPointData[id];

    o.position = AdditionalLightShadowTransform(_LightIndex, pointData.Position);


    uint idOffset = 1;
    uint last = pointData.StrandPoint + 1 == _StrandSegments ? 1 : 0;
    idOffset -= last;

    o.nextPosition = AdditionalLightShadowTransform(_LightIndex, _HairPointData[id + idOffset].Position);

    // TODO: fix this properly.
    // can't be the same position needs some bias.
    float3 offset = float3(0.001f, 0.001f, 0.001f);
    if (idOffset == 0)
        o.nextPosition = AdditionalLightShadowTransform(_LightIndex, _HairPointData[id + idOffset].Position + offset);

    float distance = (float)(pointData.StrandPoint + 1) / (float)_StrandSegments;

    o.data = float4(distance, id, pointData.StrandThickness, last);


    if (pointData.StrandPoint + 3 > _StrandSegments)        o.tanget = normalize(pointData.Position - _HairPointData[id - 2].Position);
    else                                                    o.tanget = normalize(_HairPointData[id + 2].Position - pointData.Position);

    o.uv = 0;
    o.screenSize = 0;
    o.color = float4(pointData.Color, 1);

    return o;
}

[maxvertexcount(4)]
void geom(line input p[2], inout TriangleStream<input> triStream)
{
    input g[4];
    
    // get line quad points
    float4 p1 = p[0].position;
    float4 p2 = p[1].position;

    float4 p3 = p[0].nextPosition;
    float4 p4 = p[1].nextPosition;

    float4 o1, o2, o3, o4 = 0;
    ComputeQuadPoints(p[0].data.y, p1, p2, o1, o2, o3, o4, p[0].data.z, p[1].data.z);

    float4 o1_2, o2_2, o3_2, o4_2 = 0;
    ComputeQuadPoints(p[0].data.y + 1, p3, p4, o1_2, o2_2, o3_2, o4_2, p[1].data.z, _HairPointData[p[0].data.y + 2].StrandThickness);

    float alphaFade0x = length(o2.x - o1.x);
    float alphaFade0y = length(o2.y - o1.y);

    float alphaFade1x = length(o4.x - o3.x);
    float alphaFade1y = length(o4.y - o3.y);

    // draw quad
    g[0].position = o1; 
    g[1].position = o2; 

    if (p[0].data.w != 1)
    {
        g[2].position = o1_2;
        g[3].position = o2_2;
    }

    g[0].nextPosition = p[0].nextPosition; 
    g[1].nextPosition = p[0].nextPosition; 
    g[2].nextPosition = p[1].nextPosition; 
    g[3].nextPosition = p[1].nextPosition; 

    g[0].data = p[0].data;
    g[1].data = p[0].data;
    g[2].data = p[1].data;
    g[3].data = p[1].data;

    g[0].tanget = p[0].tanget;
    g[1].tanget = p[0].tanget;
    g[2].tanget = p[1].tanget;
    g[3].tanget = p[1].tanget;

    g[0].uv = float2(0, 0);
    g[1].uv = float2(1, 0);
    g[2].uv = float2(0, 1);
    g[3].uv = float2(1, 1);

    g[0].screenSize = float2(alphaFade0x, alphaFade0y);
    g[1].screenSize = float2(alphaFade0x, alphaFade0y);
    g[2].screenSize = float2(alphaFade1x, alphaFade1y);
    g[3].screenSize = float2(alphaFade1x, alphaFade1y);
    
    g[0].color = p[0].color;
    g[1].color = p[0].color;
    g[2].color = p[1].color;
    g[3].color = p[1].color;

    if (p[0].data.w == 0)
    {
        triStream.Append(g[0]);
        triStream.Append(g[1]);
        triStream.Append(g[2]);
        triStream.Append(g[3]);
    }
}
