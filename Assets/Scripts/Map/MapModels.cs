using System;
using System.Collections.Generic;
using UnityEngine;

// Node types for the map
public enum MapNodeType
{
    Start,
    Battle,
    Elite,
    Boss

    // Add any additional node types here, but I'm not sure if we want anymore than these.
}

public enum NodeState
{
    Locked,
    Reachable,
    Cleared
}

// Data structure for a single node in the map
[Serializable]
public class MapNodeData
{
    public int id;
    public int layer;
    public Vector2 position;
    public MapNodeType type = MapNodeType.Battle;
    public NodeState state = NodeState.Locked;
    public List<int> nextNodeIds = new List<int>();
}

// Data structure for the entire map
[Serializable]
public class NodeMapData
{
    public int seed;
    public int currentNodeId = -1;
    public List<MapNodeData> nodes = new List<MapNodeData>();

    // Find a node by its ID
    public MapNodeData FindNodeById(int nodeId)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].id == nodeId)
            {
                return nodes[i];
            }
        }

        return null;
    }
}
