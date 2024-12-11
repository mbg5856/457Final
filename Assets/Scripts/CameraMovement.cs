using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float speed = 10f;           // Movement speed
    public float rotationSpeed = 300f; // Mouse rotation sensitivity

    private Rigidbody rb; // Reference to the Rigidbody

    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get input for movement (WASD or Arrow keys)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate movement direction relative to the camera's view
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // Flatten the directions (no vertical movement)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Combine inputs to get the desired movement direction
        Vector3 movement = (forward * vertical + right * horizontal) * (speed * Time.deltaTime);

        // Update Rigidbody position to respect collisions
        rb.MovePosition(rb.position + movement);

        // Get mouse input for rotation
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;

        // Rotate the camera around the Y axis (horizontal rotation)
        transform.Rotate(Vector3.up, mouseX);
    }
}