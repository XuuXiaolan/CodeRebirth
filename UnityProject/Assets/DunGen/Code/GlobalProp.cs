using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DunGen
{
    [AddComponentMenu("DunGen/Random Props/Global Prop")]
	public class GlobalProp : MonoBehaviour
	{
        public int PropGroupID = 0;
        public float MainPathWeight = 1;
        public float BranchPathWeight = 1;
        public AnimationCurve DepthWeightScale = AnimationCurve.Linear(0, 1, 1, 1);
	}
}
