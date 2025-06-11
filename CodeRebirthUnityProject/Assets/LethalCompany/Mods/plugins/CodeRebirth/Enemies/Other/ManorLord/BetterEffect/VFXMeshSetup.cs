using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace INab.CommonVFX
{
    public static class VFXMeshSetup
    {
        public static string MeshProperty = "Mesh Renderer";
        public static string SkinnedMeshProperty = "Skinned Renderer";
        public static string UseSkinnedMeshProperty = "Use Skinned Mesh";

        public static void SetupPropertyBinder(VFXPropertyBinder propertyBinder, Transform transform)
        {
            var lossScaleBinders = propertyBinder.GetPropertyBinders<VFXLossyTransformBinder>();
            
            VFXLossyTransformBinder lossyTransformBinder;

            if (lossScaleBinders.Count() == 0)
            {
                lossyTransformBinder = propertyBinder.AddPropertyBinder<VFXLossyTransformBinder>();
            }
            else
            {
                lossyTransformBinder = lossScaleBinders.First();
            }

            if (transform)
            {
                lossyTransformBinder.Target = transform;
            }
        }


        public static void SetupRenderer(Renderer renderer, VisualEffect visualEffect)
        {
            if (visualEffect.visualEffectAsset == null)
            {
                return;
            }

            bool useSkinnedMesh = false;

            if (renderer is SkinnedMeshRenderer)
            {
                visualEffect.SetSkinnedMeshRenderer(SkinnedMeshProperty, renderer as SkinnedMeshRenderer);
                useSkinnedMesh = true;
            }
            else
            {
                var filter = renderer.GetComponent<MeshFilter>();

                visualEffect.SetMesh(MeshProperty, filter.sharedMesh);
            }
            visualEffect.SetBool(UseSkinnedMeshProperty, useSkinnedMesh);

        }

    }
}