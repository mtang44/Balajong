using UnityEngine;

public class MapRunState : MonoBehaviour
{
    private static MapRunState instance;

    public static MapRunState Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MapRunState>();
                if (instance == null)
                {
                    instance = new GameObject("MapRunState").AddComponent<MapRunState>();
                }
            }
            return instance;
        }
    }

    public NodeMapData CurrentMap { get; private set; }
    public bool HasMap => CurrentMap?.nodes?.Count > 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveMap(NodeMapData mapData) => CurrentMap = mapData;

    public bool MarkCurrentNodeCleared()
    {
        if (!HasMap || CurrentMap.currentNodeId < 0) return false;
        return MarkNodeCleared(CurrentMap.currentNodeId);
    }

    public bool MarkNodeCleared(int nodeId)
    {
        if (!HasMap) return false;

        MapNodeData node = CurrentMap.FindNodeById(nodeId);
        if (node == null || node.state == NodeState.Locked) return false;

        node.state = NodeState.Cleared;
        CurrentMap.currentNodeId = node.id;

        foreach (int nextId in node.nextNodeIds)
        {
            MapNodeData next = CurrentMap.FindNodeById(nextId);
            if (next != null && next.state == NodeState.Locked)
            {
                next.state = NodeState.Reachable;
            }
        }

        return true;
    }

    public void ClearMap() => CurrentMap = null;
}
