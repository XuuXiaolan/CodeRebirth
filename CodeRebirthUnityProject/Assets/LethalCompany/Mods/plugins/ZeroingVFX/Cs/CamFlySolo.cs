using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gumou {
    public class CamFlySolo : MonoBehaviour {
        public float speed = 20f;
        public float movementSmooth = 30f;
        public float sensitivity = 2;
        public float smoothing = 10f;
        private Vector2 frameVelocity;
        private Vector2 velocity;
        private Vector3 frameMovement;
        // private Vector3 movement;

        // Start is called before the first frame update

        [Header("Lock")] 
        public KeyCode LockKey = KeyCode.F1;
        public bool bLock = false;
        
        void Start() {
            Cursor.lockState = CursorLockMode.Locked;
        }


        void Update() {
            if (Input.GetKeyDown(LockKey)) {
                bLock = !bLock;
                if (bLock) {
                    Cursor.lockState = CursorLockMode.None;
                }
                else {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

            if (bLock) {
                return;
            }
            Tanslate();
            Rotate();
        }


        void Tanslate() {
            Vector3 movement = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) movement.z += 1;
            if (Input.GetKey(KeyCode.S)) movement.z -= 1;
            if (Input.GetKey(KeyCode.A)) movement.x -= 1;
            if (Input.GetKey(KeyCode.D)) movement.x += 1;
            if (Input.GetKey(KeyCode.Q)) movement.y -= 1;
            if (Input.GetKey(KeyCode.E)) movement.y += 1;
            movement = (transform.rotation * movement);
            movement *= speed * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftShift)) movement *= 2.5f;
            frameMovement = Vector3.Lerp(frameMovement, movement, 1 / movementSmooth);
            transform.position += frameMovement;
        }

        void Rotate() {
            // Get smooth velocity.
            Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
            frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
            velocity += frameVelocity;
            velocity.y = Mathf.Clamp(velocity.y, -90, 90);

            // Rotate camera up-down and controller left-right from velocity.
            Quaternion rotation = Quaternion.AngleAxis(velocity.x, Vector3.up) *
                                  Quaternion.AngleAxis(-velocity.y, Vector3.right);
            transform.localRotation = rotation;
        }
    }
}