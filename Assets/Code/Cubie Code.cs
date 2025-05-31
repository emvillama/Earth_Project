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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is not assigned!");
        }
        else
        {
            Debug.Log("Camera Transform assigned correctly.");
        }
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal") * playerSpeed;
        float v = Input.GetAxis("Vertical") * playerSpeed;
        float inputX = Input.GetAxis("Mouse X") * mouseSens;
        float inputY = Input.GetAxis("Mouse Y") * mouseSens;

        // Handle vertical rotation (up and down)
        cameraVertRotation -= inputY;
        cameraVertRotation = Mathf.Clamp(cameraVertRotation, -90f, 90f);
        cameraTransform.localEulerAngles = Vector3.right * cameraVertRotation;

        // Handle horizontal rotation (left and right)
        transform.Rotate(Vector3.up * inputX);

        // Movement
        Vector3 movement = (transform.forward * v) + (transform.right * h);
        rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);

        // Log player position in Cubie script

    }
}