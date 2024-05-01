using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DunGen.Graph;

namespace DunGen.Editor
{
    /**
     * For simplicity, I wanted to use Unity's inspector to edit the graph objects but didn't want to
     * save each object as a seperate asset (as you would have to when deriving them from ScriptableObject.
     * 
     * So, as a hackish solution, I create a GraphObjectObserver to act as a proxy for editing the nodes in
     * the inspector. It's not a pretty solution but it works.
     */

    [Serializable]
	public class GraphObjectObserver : ScriptableObject
	{
        public DungeonFlow Flow { get; set; }
        public GraphNode Node { get; private set; }
        public GraphLine Line { get; private set; }


        public void Inspect(GraphNode node)
        {
            Node = node;
            Line = null;
        }

        public void Inspect(GraphLine line)
        {
            Line = line;
            Node = null;
        }
	}
}
