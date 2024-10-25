using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gumou.PostProcessing {
    public class SdfShape : MonoBehaviour{

    
        [Serializable]
        public struct ShapeData {
            public Vector3 position;
            public Vector3 scale;
            public Vector4 colour;
            public Vector4 custom;
            public int shapeType;
            public int operation;
            public float blendStrength;
            public int numChildren;

            public static int GetSize () {
                return sizeof (float) * 15 + sizeof (int) * 3;
            }
        }

        public ShapeData _shapeData;
        public Color Color;

        public ShapeData GetShapeDate() {
            _shapeData.position = transform.position;
            _shapeData.scale = transform.lossyScale;
            _shapeData.colour = new Vector3(Color.r,Color.g,Color.b);
            return _shapeData;

        }


    }

}
