using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cubie : MonoBehaviour
{
    public Transform cameraTransform; // Reference to the camera object
    public float mouseSens = 5f;
    float cameraVertRotation = 0f;
    public int playerSpeed = 5;
    private Rigidbody rb;
    private IInputSource input;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Input behind an interface (Stage 4 swaps keyboard for GPS/motion). Auto-adds the
        // keyboard/mouse source if none is present, so no scene wiring is needed.
        input = GetComponent<IInputSource>();
        if (input == null)
        {
            input = gameObject.AddComponent<KeyboardMouseInputSource>();
        }

        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is not assigned!");
        }
    }

    void Update()
    {
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
