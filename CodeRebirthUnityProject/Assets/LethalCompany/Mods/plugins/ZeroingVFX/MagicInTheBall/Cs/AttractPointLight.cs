using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;


namespace Gumou.MagicBall {
    public class AttractPointLight : MonoBehaviour {
        [SerializeField] private VisualEffect _effect;
    
        [SerializeField] private string _v3Name = "AttractPoint";
        private int sid_v3Name;

        // Start is called before the first frame update
        void Awake() {
            sid_v3Name = Shader.PropertyToID(_v3Name);
        }
    
    
    
    
        void Update() {
            var position =  _effect.GetVector3(sid_v3Name);
            position = _effect.transform.TransformPoint(position);
            transform.position = position;
        }
    
    
    
    
    
    
    
    
    }

}
