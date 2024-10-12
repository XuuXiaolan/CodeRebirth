// Shader to replace the SampleGradient node

void SampleGradient_float(
                   float4 color1,
                   float4 color2,
                   float4 color3,
                   float4 color4,
                   float location1,
                   float location2,
                   float location3,
                   float location4,
                   float inputValue,
                   out float4 outFloat)
{
    if (inputValue < location1)
    {
        outFloat = color1;
    }
    else if (inputValue < location2)
    {
        float pos = (inputValue - location1) / (location2 - location1);
        outFloat = lerp(color1, color2, pos);
    }
    else if (inputValue < location3)
    {
        float pos = (inputValue - location2) / (location3 - location2);
        outFloat = lerp(color2, color3, pos);
    }
    else if (inputValue < location4)
    {
        float pos = (inputValue - location3) / (location4 - location3);
        outFloat = lerp(color3, color4, pos);
    }
    else
    {
        outFloat = color4;
    }
}