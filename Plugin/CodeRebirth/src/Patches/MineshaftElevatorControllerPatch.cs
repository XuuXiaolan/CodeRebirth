namespace CodeRebirth.src.Patches;
public static class MineshaftElevatorControllerPatch
{
    public static void Init()
    {
        On.MineshaftElevatorController.Update += MineshaftElevatorController_Update;
    }

    private static void MineshaftElevatorController_Update(On.MineshaftElevatorController.orig_Update orig, MineshaftElevatorController self)
    {
        orig(self);
        if (self.elevatorFinishedMoving)
        {
            if (self.movingDownLastFrame)
            {
                self.elevatorIsAtBottom = true;
            }
            else
            {
                self.elevatorIsAtBottom = false;
            }
        }
    }
}