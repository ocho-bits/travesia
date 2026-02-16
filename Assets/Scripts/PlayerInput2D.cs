using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInput2D : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
    public bool JumpHeld { get; private set; }

    void Update()
    {
        // Movement
        float x = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        }
        if (Gamepad.current != null)
            x = Mathf.Abs(Gamepad.current.leftStick.x.ReadValue()) > 0.1f ? Gamepad.current.leftStick.x.ReadValue() : x;

        Move = new Vector2(Mathf.Clamp(x, -1f, 1f), 0f);

        // Jump
        bool jumpDown = false;
        bool jumpHeld = false;

        if (Keyboard.current != null)
        {
            jumpDown |= Keyboard.current.spaceKey.wasPressedThisFrame;
            jumpHeld |= Keyboard.current.spaceKey.isPressed;
        }
        if (Gamepad.current != null)
        {
            jumpDown |= Gamepad.current.buttonSouth.wasPressedThisFrame;
            jumpHeld |= Gamepad.current.buttonSouth.isPressed;
        }

        JumpPressedThisFrame = jumpDown;
        JumpHeld = jumpHeld;
    }

    void LateUpdate()
    {
        // Consume one-frame flags
        JumpPressedThisFrame = false;
    }
}