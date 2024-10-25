using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Gumou.MagicBall {
    public class PointRotateWithMouse : MonoBehaviour {
        // [SerializeField] private Transform origin;
        [SerializeField] private float sensitivity = 2f;
        [SerializeField] private float smoothing = 10;
        private Vector2 frameVelocity;
        private Vector2 velocity;

        [SerializeField] private VisualEffect _effect;
        [SerializeField] private string _v3Name = "AttractPoint";
        private int sid_v3Name;

    
    
        // Start is called before the first frame update
        void Awake() {
            sid_v3Name = Shader.PropertyToID(_v3Name);
        }

        // Update is called once per frame
        void Update()
        {
            Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
            frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
            velocity += frameVelocity;
            velocity.y = Mathf.Clamp(velocity.y, -90, 90);

            // Rotate camera up-down and controller left-right from velocity.
            Quaternion rotation = Quaternion.AngleAxis(velocity.x, Vector3.up) *
                                  Quaternion.AngleAxis(-velocity.y, Vector3.right);

            Vector3 pos = rotation * Vector3.forward;
        
        
            _effect.SetVector3(sid_v3Name,pos);
        }
    }

}
