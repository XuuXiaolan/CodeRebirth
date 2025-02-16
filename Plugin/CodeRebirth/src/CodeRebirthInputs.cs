using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using UnityEngine.InputSystem;

namespace CodeRebirth.src;
public class IngameKeybinds : LcInputActions
{
    [InputAction("<Keyboard>/w", Name = "HoverForward")]
    public InputAction HoverForward { get; set; } = null!;

    [InputAction("<Keyboard>/shift", Name = "SprintForward")]
    public InputAction SprintForward { get; set; } = null!;

    [InputAction("<Keyboard>/a", Name = "HoverLeft")]
    public InputAction HoverLeft { get; set; } = null!;

    [InputAction("<Keyboard>/s", Name = "HoverBackward")]
    public InputAction HoverBackward { get; set; } = null!;

    [InputAction("<Keyboard>/d", Name = "HoverRight")]
    public InputAction HoverRight { get; set; } = null!;

    [InputAction("<Keyboard>/space", Name = "HoverUp")]
    public InputAction HoverUp { get; set; } = null!;

    [InputAction("<Keyboard>/c", Name = "DropHoverboard")]
    public InputAction DropHoverboard { get; set; } = null!;

    [InputAction("<Keyboard>/f", Name = "SwitchMode")]
    public InputAction SwitchMode { get; set; } = null!;

    [InputAction(MouseControl.Delta, Name = "MouseDelta")]
    public InputAction MouseDelta { get; set; } = null!;
}