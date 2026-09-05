using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float flySpeed = 12f;
    [Tooltip("How much to multiply the speed by when holding Shift (e.g., 0.4 means 40% speed)")]
    public float slowMultiplier = 0.4f;
    public float movementSmoothTime = 0.18f;

    [Header("Look Settings")]
    public float lookSensitivity = 0.08f;
    public float lookSmoothTime = 0.05f;

    private float rotationX;
    private float rotationY;

    private Vector3 currentVelocity;
    private Vector3 velocitySmoothRef;

    private Vector2 currentLookDelta;
    private Vector2 lookSmoothRef;

    private void Start()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationX = rot.y;
        rotationY = rot.x;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        HandleMovement(dt);
        HandleMouseLook(dt);
    }

    private void HandleMovement(float dt)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector3 direction = Vector3.zero;

        if (keyboard.wKey.isPressed) direction += transform.forward;
        if (keyboard.sKey.isPressed) direction -= transform.forward;
        if (keyboard.aKey.isPressed) direction -= transform.right;
        if (keyboard.dKey.isPressed) direction += transform.right;

        if (keyboard.eKey.isPressed) direction += Vector3.up;
        if (keyboard.qKey.isPressed) direction -= Vector3.up;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        float speed = flySpeed;

        // Changed this line to apply the slowing multiplier instead of a boost
        if (keyboard.shiftKey.isPressed)
            speed *= slowMultiplier;

        // If you prefer to keep the boost logic variable names and just divide instead, 
        // you could use: speed /= boostMultiplier;

        Vector3 targetVelocity = direction * speed;

        currentVelocity = Vector3.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref velocitySmoothRef,
            movementSmoothTime,
            Mathf.Infinity,
            dt
        );

        transform.position += currentVelocity * dt;
    }

    private void HandleMouseLook(float dt)
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 rawDelta = mouse.delta.ReadValue();

            currentLookDelta = Vector2.SmoothDamp(
                currentLookDelta,
                rawDelta,
                ref lookSmoothRef,
                lookSmoothTime,
                Mathf.Infinity,
                dt
            );

            rotationX += currentLookDelta.x * lookSensitivity;
            rotationY -= currentLookDelta.y * lookSensitivity;

            rotationY = Mathf.Clamp(rotationY, -85f, 85f);

            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }
        else
        {
            currentLookDelta = Vector2.zero;
            lookSmoothRef = Vector2.zero;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}