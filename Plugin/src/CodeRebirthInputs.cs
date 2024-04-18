using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace CodeRebirth.Keybinds;
public class IngameKeybinds : LcInputActions {
    [InputAction("<Keyboard>/p", Name = "ExampleKeybind")]
    public InputAction Example { get; set; }
    [InputAction("<Keyboard>/y", Name = "UseWalletKeybind")]
    public InputAction UseWallet { get; set; }
}