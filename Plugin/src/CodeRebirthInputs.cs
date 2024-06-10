using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace CodeRebirth.Keybinds;
public class IngameKeybinds : LcInputActions {
    [InputAction("<Keyboard>/i", Name = "HoverForward")]
    public InputAction HoverForward { get; set; }

    [InputAction("<Keyboard>/j", Name = "HoverLeft")]
    public InputAction HoverLeft { get; set; }

    [InputAction("<Keyboard>/k", Name = "HoverBackward")]
    public InputAction HoverBackward { get; set; }

    [InputAction("<Keyboard>/l", Name = "HoverRight")]
    public InputAction HoverRight { get; set; }

    [InputAction("<Keyboard>/space", Name = "HoverUp")]
    public InputAction HoverUp { get; set; }

    [InputAction("<Keyboard>/c", Name = "DropHoverboard")]
    public InputAction DropHoverboard { get; set; }

    [InputAction("<Keyboard>/f", Name = "SwitchMode")]
    public InputAction SwitchMode { get; set; }
}