using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gumou;
using UnityEngine;

namespace Gumou.PostProcessing {
    public class ShapeSdfVolume : SingletonBehaviour<ShapeSdfVolume> {
    

        public List<SdfShape> _shapes;
    

        public SdfShape.ShapeData[] GetShapeDatas() {
            var shapes = _shapes.Select((x) => x.GetShapeDate());
            return shapes.ToArray();
        }

        public void AddShape(SdfShape shape) {
            if (_shapes.Contains(shape) == false) {
                _shapes.Add(shape); 
            }
        }
    
    

    
    }

}
