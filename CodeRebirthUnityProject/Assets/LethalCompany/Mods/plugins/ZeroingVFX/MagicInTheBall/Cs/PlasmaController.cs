using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;



namespace Gumou.MagicBall {
    
public class PlasmaController : MonoBehaviour {
    
    
    [SerializeField] private Transform _metal;
    [SerializeField] private float metalRadius = 1.4f;
    [SerializeField] private VisualEffect _effect;

    private Camera _camera;

    private bool keydown = false;
    private bool selected = false;
    
    
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float smoothing = 10;
    private Vector2 frameVelocity;
    private Vector2 velocity;

    [SerializeField] private string _v3Name = "AttractPoint";
    private int sid_v3Name;
    
    
    [SerializeField] private Light _light;


    // Start is called before the first frame update
    void Awake() {
        sid_v3Name = Shader.PropertyToID(_v3Name);
        _camera = Camera.main;
    }

    private void Start() {
        keydown = selected= false;
        _metal.transform.localPosition = _metal.localPosition.normalized * 2f;
        _metal.transform.localScale = Vector3.one * 0f;
        _effect.Stop();
        _light.gameObject.SetActive(false);
    }


    void Update() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            keydown = true;
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray,out var hit)) {
                var b = hit.collider.gameObject == gameObject;
                if (b) {
                    selected = true;
                    _metal.transform.localPosition = _metal.localPosition.normalized * metalRadius;
                    _metal.transform.localScale = Vector3.one * 0.2f;
                    _effect.Play();
                    _light.gameObject.SetActive(true);
                }
            }    
        }




        if (Input.GetKeyUp(KeyCode.Mouse0)) {
            keydown = selected= false;
            _metal.transform.localPosition = _metal.localPosition.normalized * 2f;
            _metal.transform.localScale = Vector3.one * 0f;
            _effect.Stop();
            _light.gameObject.SetActive(false);

        }


        if (keydown && selected) {
            Rotate();
        }
    }
    
    
    
    
    
    void Rotate()
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

        Vector3 wPos = _effect.transform.TransformPoint(pos * metalRadius);
        _metal.position = wPos;
    }
    
    
    
}

}