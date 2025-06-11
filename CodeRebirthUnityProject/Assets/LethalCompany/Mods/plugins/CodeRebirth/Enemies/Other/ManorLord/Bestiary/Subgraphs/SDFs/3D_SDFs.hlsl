#ifndef CUSTOMMASKS_INCLUDED
#define CUSTOMMASKS_INCLUDED

// From https://iquilezles.org/articles/distfunctions/

float3 PositionRotated(float3 posDiff, float3 right, float3 up, float3 forward)
{
    return float3(dot(posDiff, right), dot(posDiff, up), dot(posDiff, forward));
}

void Sphere_float(float3 p, float s, out float Out)
{
    Out = length(p) - s;
}

float Sphere(float3 p, float s)
{
    return length(p) - s;
}

void Plane_float(float3 p,float3 wp, float3 up, out float Out)
{
    float3 diff = p - wp;
    Out = dot(diff, up);
}

float Plane(float3 p, float3 wp, float3 up)
{
    float3 diff = p - wp;
    return dot(diff, up);
}

void Box_float(float3 p, float3 b, out float Out)
{
    float3 q = abs(p) - b;
    Out = length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float Box(float3 p, float3 b)
{
    float3 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

void SolidAngle_float(float3 p, float2 c, float ra, out float Out)
{
     // c is the sin/cos of the angle
    float2 q = float2(length(p.xz), -p.y); // Invert the sign of p.y
    float l = length(q) - ra;
    float m = length(q - c * clamp(dot(q, c), 0.0, ra));
    Out = max(l, m * sign(c.y * q.x - c.x * q.y));
}

float SolidAngle(float3 p, float2 c, float ra)
{
    // c is the sin/cos of the angle
    float2 q = float2(length(p.xz), -p.y); // Invert the sign of p.y
    float l = length(q) - ra;
    float m = length(q - c * clamp(dot(q, c), 0.0, ra));
    return max(l, m * sign(c.y * q.x - c.x * q.y));
}

void Ellipsoid_float(float3 p, float3 r, out float Out)
{
    float k0 = length(p / r);
    float k1 = length(p / (r * r));
    
    // To keep negative scales working
    if (r.x < 0.0 || r.y < 0.0 || r.z < 0.0)
    {
        k0 *= -1;
    }
    
    Out = k0 * (k0 - 1.0) / k1;
}

float Ellipsoid(float3 p, float3 r)
{
    float3 absR = abs(r);
    float k0 = length(p / absR);
    float k1 = length(p / (r * r));
    
    // To keep negative scales working
    if (r.x < 0.0 || r.y < 0.0 || r.z < 0.0)
    {
        k0 *= -1;
    }

    return k0 * (k0 - 1.0) / k1;
}

float dot2(in float3 v)
{
    return dot(v, v);
}

void RoundCone_float(float3 p, float3 a, float3 b, float r1, float r2, out float Out)
{
  // Sampling-independent computations (only depend on shape)
    float3 ba = b - a;
    float l2 = dot(ba, ba);
    float rr = r1 - r2;
    float a2 = l2 - rr * rr;
    float il2 = 1.0 / l2;
    
    // Sampling-dependent computations
    float3 pa = p - a;
    float y = dot(pa, ba);
    float z = y - l2;
    float x2 = dot2(pa * l2 - ba * y);
    float y2 = y * y * l2;
    float z2 = z * z * l2;

   // Single square root!
    float k = sign(rr) * rr * rr * x2;
    if (sign(z) * a2 * z2 > k)
        Out = sqrt(x2 + z2) * il2 - r2;
    else if (sign(y) * a2 * y2 < k)
        Out = sqrt(x2 + y2) * il2 - r1;
    else
        Out = (sqrt(x2 * a2 * il2) + y * rr) * il2 - r1;
}

float RoundCone(float3 p, float3 a, float3 b, float r1, float r2)
{
    // Sampling-independent computations (only depend on shape)
    float3 ba = b - a;
    float l2 = dot(ba, ba);
    float rr = r1 - r2;
    float a2 = l2 - rr * rr;
    float il2 = 1.0 / l2;

    // Sampling-dependent computations
    float3 pa = p - a;
    float y = dot(pa, ba);
    float z = y - l2;
    float x2 = dot2(pa * l2 - ba * y);
    float y2 = y * y * l2;
    float z2 = z * z * l2;

    // Single square root!
    float k = sign(rr) * rr * rr * x2;
    if (sign(z) * a2 * z2 > k)
        return sqrt(x2 + z2) * il2 - r2;
    else if (sign(y) * a2 * y2 < k)
        return sqrt(x2 + y2) * il2 - r1;
    else
        return (sqrt(x2 * a2 * il2) + y * rr) * il2 - r1;
}

#endif