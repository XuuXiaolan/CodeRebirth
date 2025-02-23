using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public static class AnimationCurveUtil
{
    public enum TangentMode
    {
        Free,
        Smooth,
        Linear
    }

    public static void SetKeyTangentMode(AnimationCurve curve, int index, TangentMode mode)
    {
        if (index < 0 || index >= curve.length) return;

        Keyframe key = curve[index];
        switch (mode)
        {
            case TangentMode.Free:
                key.inTangent = float.PositiveInfinity;
                key.outTangent = float.PositiveInfinity;
                break;
            case TangentMode.Smooth:
                float dx1 = key.time - (index > 0 ? curve[index - 1].time : key.time - 1f);
                float dy1 = key.value - (index > 0 ? curve[index - 1].value : key.value);
                key.inTangent = dy1 / dx1;

                float dx2 = (index < curve.length - 1 ? curve[index + 1].time : key.time + 1f) - key.time;
                float dy2 = (index < curve.length - 1 ? curve[index + 1].value : key.value) - key.value;
                key.outTangent = dy2 / dx2;
                break;
            case TangentMode.Linear:
                key.inTangent = 0;
                key.outTangent = 0;
                break;
        }
        curve.MoveKey(index, key);
    }
}