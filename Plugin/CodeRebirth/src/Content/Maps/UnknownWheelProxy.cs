using System;

namespace CodeRebirth.src.Content.Maps;

public class UnknownWheelProxy : BearTrapWheelProxy
{
    public Action? PunctureWheelAction;
    public Action? SetupWheelAction;

    public override void SetupWheel()
    {
        SetupWheelAction?.Invoke();
    }

    public override void PunctureWheel()
    {
        base.PunctureWheel();
        PunctureWheelAction?.Invoke();
    }
}