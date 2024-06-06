using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace CodeRebirth.Keybinds;
public class IngameKeybinds : LcInputActions {
    [InputAction("<Keyboard>/w", Name = "HoverForward")]
    public InputAction HoverForward { get; set; }

    [InputAction("<Keyboard>/a", Name = "HoverLeft")]
    public InputAction HoverLeft { get; set; }

    [InputAction("<Keyboard>/s", Name = "HoverBackward")]
    public InputAction HoverBackward { get; set; }

    [InputAction("<Keyboard>/d", Name = "HoverRight")]
    public InputAction HoverRight { get; set; }

    [InputAction("<Keyboard>/space", Name = "HoverUp")]
    public InputAction HoverUp { get; set; }

    [InputAction("<Keyboard>/c", Name = "DropHoverboard")]
    public InputAction DropHoverboard { get; set; }
    [InputAction("<Keyboard>/y", Name = "IncreaseHover")]
    public InputAction IncreaseHover { get; set; }

    [InputAction("<Keyboard>/h", Name = "DecreaseHover")]
    public InputAction DecreaseHover { get; set; }

    [InputAction("<Keyboard>/p", Name = "TurnOnHoverboard")]
    public InputAction TurnOnHoverboard { get; set; }

    [InputAction("<Keyboard>/f", Name = "SwitchMode")]
    public InputAction SwitchMode { get; set; }
}