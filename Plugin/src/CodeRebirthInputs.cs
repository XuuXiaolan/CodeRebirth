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
}