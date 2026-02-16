using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInput2D : MonoBehaviour
{
    public float MoveX { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
    public bool JumpHeld { get; private set; }

    void Update()
    {
        // Move
        float x = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        }

        if (Gamepad.current != null)
        {
            float stick = Gamepad.current.leftStick.x.ReadValue();
            if (Mathf.Abs(stick) > 0.1f) x = stick;
        }

        MoveX = Mathf.Clamp(x, -1f, 1f);

        // Jump
        JumpPressedThisFrame = false;
        JumpHeld = false;

        if (Keyboard.current != null)
        {
            JumpPressedThisFrame |= Keyboard.current.spaceKey.wasPressedThisFrame;
            JumpHeld |= Keyboard.current.spaceKey.isPressed;
        }

        if (Gamepad.current != null)
        {
            JumpPressedThisFrame |= Gamepad.current.buttonSouth.wasPressedThisFrame;
            JumpHeld |= Gamepad.current.buttonSouth.isPressed;
        }
    }
}