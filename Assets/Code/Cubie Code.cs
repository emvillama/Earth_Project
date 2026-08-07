using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cubie : MonoBehaviour
{
    public Transform cameraTransform; // Reference to the camera object
    public float mouseSens = 5f;
    float cameraVertRotation = 0f;
    public float playerSpeed = 1.5f; // ~walking pace (m/s); tune in the Inspector
    private Rigidbody rb;
    private IInputSource input;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Cursor lock is handled in Update — kept free while the main menu is up so it stays clickable.

        // Input behind an interface (Stage 4 swaps keyboard for GPS/motion). Auto-adds the
        // keyboard/mouse source if none is present, so no scene wiring is needed.
        input = GetComponent<IInputSource>();
        if (input == null)
        {
            // Phone (and the editor, so we can preview with the mouse) → on-screen joysticks;
            // standalone desktop builds → keyboard + mouse.
            if (Application.isMobilePlatform || Application.isEditor)
                input = gameObject.AddComponent<TouchControls>();
            else
                input = gameObject.AddComponent<KeyboardMouseInputSource>();
        }

        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is not assigned!");
        }
    }

    void Update()
    {
        // While the main menu is up: free the cursor (so its buttons are clickable) and freeze the
        // player. Once the game starts, lock/hide the cursor for mouse-look. Touch is unaffected.
        if (!GameConfig.Configured)
        {
            if (Cursor.lockState != CursorLockMode.None) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

        float h = input.Horizontal * playerSpeed;
        float v = input.Vertical * playerSpeed;
        float inputX = input.LookX * mouseSens;
        float inputY = input.LookY * mouseSens;

        // Handle vertical rotation (up and down)
        cameraVertRotation -= inputY;
        cameraVertRotation = Mathf.Clamp(cameraVertRotation, -90f, 90f);
        cameraTransform.localEulerAngles = Vector3.right * cameraVertRotation;

        // Handle horizontal rotation (left and right)
        transform.Rotate(Vector3.up * inputX);

        // Movement
        Vector3 movement = (transform.forward * v) + (transform.right * h);
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }
}
