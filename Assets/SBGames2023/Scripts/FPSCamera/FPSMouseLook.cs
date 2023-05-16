using UnityEngine;

public class FPSMouseLook : MonoBehaviour
{
    public float sensitivity = 2f; // Mouse sensitivity
    private float xRotation = 0f;
    private float yRotation = 0f;
    

    private void Start()
    {
        // Lock the cursor to the game window and hide it
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // Rotate the player's body horizontally
        //playerBody.Rotate(Vector3.up * mouseX);

        // Rotate the camera vertically
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Clamp the vertical rotation to avoid flipping

        yRotation += mouseX;
        //yRotation = Mathf.Clamp(yRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}