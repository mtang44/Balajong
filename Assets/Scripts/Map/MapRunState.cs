using UnityEngine;

public class MapRunState : MonoBehaviour
{
    private static MapRunState instance;

    // Singleton instance of the MapRunState
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

    // Saves the current map data
    public void SaveMap(NodeMapData mapData) => CurrentMap = mapData;

    // Marks the current node as cleared
    public bool MarkCurrentNodeCleared()
    {
        if (!HasMap || CurrentMap.currentNodeId < 0) return false;
        return MarkNodeCleared(CurrentMap.currentNodeId);
    }

    // Marks the specified node as cleared
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

    public int LoopCount { get; private set; }

    public void IncrementLoop() { LoopCount++; }

    public void ResetLoopCount() { LoopCount = 0; }

    public void ClearMap() => CurrentMap = null;
}
