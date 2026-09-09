using UnityEngine;
using UnityEngine.InputSystem;

public class BattleKeyboardManager : MonoBehaviour
{
    public static BattleKeyboardManager i;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        i = this;

        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.BattleBinds.ToggleGroup.performed += ToggleGroup;
        inputActions.BattleBinds.Select.performed += ctx => MouseController.i.BeginSelection();
        inputActions.BattleBinds.Select.canceled += ctx => MouseController.i.EndSelection();
    }

    public bool IsCtrlModifierHeld()
    {
        if (inputActions != null)
        {
            return inputActions.BattleBinds.CtrlClick.IsPressed();
        }

        return Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
    }

    private void ToggleGroup(InputAction.CallbackContext ctx)
    {
        BattleUI.i.AttemptGrouping();
    }
}