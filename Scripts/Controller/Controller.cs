using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof (Rigidbody))]
public class Controller : MonoBehaviour
{
    [System.Serializable]
    public class MovementSettings {
        public float speed = 20.0f;
        [HideInInspector] public float currentTargetSpeed = 8.0f;
        public float runMultiplier = 2.0f; 
        public KeyCode runKey = KeyCode.LeftShift;

        private bool running = false;

        public void UpdateDesiredTargetSpeed(Vector2 input) {

            if (input == Vector2.zero) {
                return;
            }

            if (input.x > 0 || input.x < 0) {
                // Strafe
                currentTargetSpeed = speed;
            }
            if (input.y < 0) {
                // Backwards
                currentTargetSpeed = speed;
            }
            if (input.y > 0) {
                // Forwards
                currentTargetSpeed = speed;
            }

            if (Input.GetKey(runKey))
            {
                currentTargetSpeed *= runMultiplier;
                running = true;
            }
            else
            {
                running = false;
            }
        }
    }   
    

    public Camera cam;
    public MovementSettings movementSettings = new MovementSettings();
    public MouseLook mouseLook = new MouseLook();

    private Rigidbody rigidBody;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        mouseLook.Init(transform, cam.transform);
    }

    void Update()
    {
        RotateView();
    }

    private Vector2 GetInput() {
        Vector2 input = new Vector2
        {
            x = Input.GetAxis("Horizontal"),
            y = Input.GetAxis("Vertical")
        };
        movementSettings.UpdateDesiredTargetSpeed(input);
        return input;
    }

    private void RotateView()
    {
        //avoids the mouse looking if the game is effectively paused
        if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

        // get the rotation before it's changed
        float oldYRotation = transform.eulerAngles.y;

        mouseLook.LookRotation(transform, cam.transform);
    }

    private void FixedUpdate() {
        Vector2 input = GetInput();
        
        if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon)) {
            Vector3 desiredMove = cam.transform.forward * input.y + cam.transform.right * input.x;

            desiredMove.x = desiredMove.x * movementSettings.currentTargetSpeed;
            desiredMove.z = desiredMove.z * movementSettings.currentTargetSpeed;
            desiredMove.y = desiredMove.y * movementSettings.currentTargetSpeed;

            if (rigidBody.velocity.sqrMagnitude <
                (movementSettings.currentTargetSpeed * movementSettings.currentTargetSpeed))
            {
                rigidBody.AddForce(desiredMove, ForceMode.Impulse);
            }
        }

        rigidBody.drag = 5f;
    }
}
