using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapNodeType
{
    Start,
    Battle,
    Elite,
    Shop,
    Rest,
    Event,
    Treasure,
    Boss
}

public enum NodeState
{
    Locked,
    Reachable,
    Cleared
}

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

[Serializable]
public class NodeMapData
{
    public int seed;
    public int currentNodeId = -1;
    public List<MapNodeData> nodes = new List<MapNodeData>();

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
