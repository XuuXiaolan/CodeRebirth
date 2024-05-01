using System.Collections.Generic;

namespace DunGen
{
    public class DungeonGraph
    {
        public readonly List<DungeonGraphNode> Nodes = new List<DungeonGraphNode>();
        public readonly List<DungeonGraphConnection> Connections = new List<DungeonGraphConnection>();


        public DungeonGraph(Dungeon dungeon)
        {
            Dictionary<Tile, DungeonGraphNode> nodeMap = new Dictionary<Tile, DungeonGraphNode>();

            foreach (var tile in dungeon.AllTiles)
            {
                var node = new DungeonGraphNode(tile);
                nodeMap[tile] = node;
                Nodes.Add(node);
            }

            foreach (var conn in dungeon.Connections)
            {
                var nodeConn = new DungeonGraphConnection(nodeMap[conn.A.Tile], nodeMap[conn.B.Tile], conn.A, conn.B);
                Connections.Add(nodeConn);
            }
        }
    }
}
