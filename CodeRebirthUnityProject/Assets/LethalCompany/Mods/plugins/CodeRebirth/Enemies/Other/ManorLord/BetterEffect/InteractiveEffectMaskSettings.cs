using UnityEngine;

namespace INab.Common
{
    [CreateAssetMenu(fileName = "Data", menuName = "Interactive Effects/Mask Settings", order = 1)]
    public class InteractiveEffectMaskSettings : ScriptableObject
    {
        public Color Color = new Color(0.1f, 0.97f, 0.1f, 0.7f);
        public float NormalSize = 1;
        public float PlaneSize = 1;

        public Material PreviewMaterial;
        public Mesh PreviewPlaneMesh;
        public Mesh PreviewSphereMesh;
        public Mesh PreviewBoxMesh;
    }
}