using System;
using System.Collections;
using System.Collections.Generic;
using Gumou;
using UnityEngine;

namespace Gumou.PostProcessing {
    [RequireComponent(typeof(ParticleSystem)),ExecuteAlways]
    public class ParticleSdfVolume : SingletonBehaviour<ParticleSdfVolume> {
        public ParticleSystem _particleSys;
        private ParticleSystem.Particle[] _particles;
        public SdfShape.ShapeData[] shapes;
    
        private void Awake() {
            _particleSys = GetComponent<ParticleSystem>();
        
        }


        void Update() {
            var particleCount = _particleSys.particleCount;
            _particles = new ParticleSystem.Particle[particleCount];
            int p = _particleSys.GetParticles(_particles, particleCount);
            // DebugGUI.S.LogText($"{p}, , {_particles.Length}");
        }


        public SdfShape.ShapeData[] GetShapeDatas() {
            if (_particles == null || _particles.Length == 0) {
                return null;
            }
            shapes = new SdfShape.ShapeData[_particles.Length];
            for (int i = 0; i < _particles.Length; i++) {
                SdfShape.ShapeData shape = new SdfShape.ShapeData();
                var p = _particles[i];
                shape.position = p.position;
                var size = p.GetCurrentSize(_particleSys);
                var color = p.GetCurrentColor(_particleSys);
                shape.scale = new Vector3(size, size, size);
                shape.shapeType = 0;
                shape.blendStrength = 0.3f;
                shape.colour = new Vector4(color.r/255f, color.g/255f, color.b/255f, color.a/255f);
                shapes[i] = shape;
            }

            return shapes;
        }
    }



}
