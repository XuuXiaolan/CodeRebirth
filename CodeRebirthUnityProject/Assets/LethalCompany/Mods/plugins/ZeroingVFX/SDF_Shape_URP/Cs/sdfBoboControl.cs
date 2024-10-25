using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Gumou.PostProcessing {
    public class sdfBoboControl : MonoBehaviour {
    private ShapeSdfVolume _shapeSdfVolume;

    [SerializeField] private List<SdfShape> balls = new List<SdfShape>();
    private List<(SdfShape b0, SdfShape line, SdfShape b1)> chain = new List<(SdfShape b0, SdfShape s, SdfShape b1)>();
    [SerializeField] private bool loop = false;
    [SerializeField,Min(0)] public float lineRadius = 0.3f;
    [SerializeField,Min(0)] public float lineBlend = 0.3f;
    public bool CreateChainAwake = true;

    void Start() {
        _shapeSdfVolume = ShapeSdfVolume.S;
        if (CreateChainAwake) {
            CreateChain();
        }
    }

    
    void Update() {
        for (int i = 0; i < chain.Count; i++) {
            var tup = chain[i];
            UpdateCapsuleData(tup);
        }
    }

    public void AddBall(SdfShape ball) {
        balls.Add(ball);
        if (balls.Count < 2) {
            return;
        }
        var line=  CreateCapsule( balls[^2],ball);
        _shapeSdfVolume.AddShape(line);
        chain.Add((balls[^2],line,ball));
    }
    
    void CreateChain() {
        if (balls == null) {
            return;
        }
        if (balls.Count < 2) {
            return;
        }
        chain.Clear();
        for (int i = 1; i < balls.Count; i++) {
            var line=  CreateCapsule( balls[i-1],balls[i]);
            _shapeSdfVolume.AddShape(line);
            chain.Add((balls[i-1],line,balls[i]));
        }
        if (loop) {
            var line=  CreateCapsule( balls[^1],balls[0]);
            _shapeSdfVolume.AddShape(line);
            chain.Add((balls[^1],line,balls[0]));
        }
    }
    SdfShape CreateCapsule(SdfShape b1,SdfShape b2) {
        var go = new GameObject("sdf_Capsule");
        go.transform.SetParent(transform);
        var shape = go.AddComponent<SdfShape>();
        shape.transform.localScale = b2.transform.position-b1.transform.position;
        shape.transform.position = b1.transform.position;
        shape._shapeData.custom.w = lineRadius;
        shape._shapeData.blendStrength = lineBlend;
        shape._shapeData.shapeType = 2;
        return shape;
    }

    void UpdateCapsuleData((SdfShape b0, SdfShape line, SdfShape b1) tup) {
        var b1 = tup.b0;
        var b2 = tup.b1;
        var shape = tup.line;
        shape.transform.localScale = b2.transform.position-b1.transform.position;
        shape.transform.position = b1.transform.position;
        shape._shapeData.custom.w = lineRadius;
        shape._shapeData.blendStrength = lineBlend;
    }
}
}
