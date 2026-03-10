using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NodeMap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapConfig config;
    [SerializeField] private Transform mapRoot;
    [SerializeField] private MapNodeView nodeViewPrefab;
    [SerializeField] private Material lineMaterial;

    [Header("Interaction")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private LayerMask nodeClickMask = ~0;
    [SerializeField] private bool ignoreClicksOverUi = true;

    [Header("2D Visuals")]
    [SerializeField] private Sprite defaultNodeSprite;
    [SerializeField] private int nodeSortingOrder = 10;
    [SerializeField] private int lineSortingOrder = 0;
    [SerializeField] private Sprite playerMarkerSprite;
    [SerializeField] private Color playerMarkerColor = new Color(0.2f, 0.95f, 1f, 1f);
    [SerializeField] private Vector3 playerMarkerOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private float playerMarkerScale = 0.55f;
    [SerializeField] private int playerMarkerSortingOrder = 20;

    [Header("Runtime")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool loadFromRunState = true;
    [SerializeField] private int fixedSeed = -1;
    [SerializeField] private bool verboseLogging = false;

    private NodeMapData mapData;
    private System.Random random;
    private Material runtimeLineMaterial;
    private Sprite generatedCircleSprite;
    private GameObject playerMarkerObject;
    private SpriteRenderer playerMarkerRenderer;
    private bool warnedMissingCamera;

    private readonly Dictionary<int, MapNodeData> nodesById = new Dictionary<int, MapNodeData>();
    private readonly Dictionary<int, MapNodeView> viewsById = new Dictionary<int, MapNodeView>();
    private readonly List<ConnectionVisual> connectionVisuals = new List<ConnectionVisual>();

    private struct ConnectionVisual
    {
        public int fromId;
        public int toId;
        public LineRenderer line;
    }

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("NodeMap: MapConfig reference is missing.");
            return;
        }

        if (!generateOnStart)
        {
            return;
        }

        if (Application.isPlaying && loadFromRunState && MapRunState.Instance.HasMap)
        {
            mapData = MapRunState.Instance.CurrentMap;
            BuildLookup();
            RebuildVisuals();
            return;
        }

        GenerateNewMap();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!TryGetPointerDownThisFrame(out Vector2 screenPosition))
        {
            return;
        }

        if (ignoreClicksOverUi && IsPointerOverUi())
        {
            return;
        }

        Camera cameraToUse = ResolveInputCamera();
        if (cameraToUse == null)
        {
            if (!warnedMissingCamera)
            {
                warnedMissingCamera = true;
                Debug.LogWarning("NodeMap: No input camera found. Assign Input Camera or tag a camera as MainCamera.");
            }

            return;
        }

        warnedMissingCamera = false;
        MapNodeView clickedNode = RaycastNodeView(cameraToUse, screenPosition);
        if (clickedNode == null)
        {
            return;
        }

        OnNodeClicked(clickedNode.NodeId);
    }

    [ContextMenu("Generate New Map")]
    public void GenerateNewMap()
    {
        int seed = fixedSeed >= 0 ? fixedSeed : UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        GenerateNewMap(seed);
    }

    public void GenerateNewMap(int seed)
    {
        if (config == null)
        {
            Debug.LogError("NodeMap: Cannot generate map because config is missing.");
            return;
        }

        random = new System.Random(seed);
        mapData = new NodeMapData
        {
            seed = seed,
            currentNodeId = -1
        };

        List<List<MapNodeData>> layers = CreateLayers();
        BuildGuaranteedPaths(layers);
        EnsureNodeConnectivity(layers);
        AddExtraConnections(layers);
        OptimizeNodePositionsForEdgeCrossing(layers);
        AssignNodeTypes(layers);
        InitializeNodeStates(layers);

        BuildLookup();
        RebuildVisuals();
        SaveRuntimeState();

        if (verboseLogging)
        {
            Debug.Log($"NodeMap: Generated seed {seed} with {mapData.nodes.Count} nodes.");
        }
    }

    [ContextMenu("Complete Current Node")]
    public void CompleteCurrentNode()
    {
        if (mapData == null || mapData.currentNodeId < 0)
        {
            return;
        }

        CompleteNode(mapData.currentNodeId);
    }

    public void OnNodeClicked(int nodeId)
    {
        if (!nodesById.TryGetValue(nodeId, out MapNodeData node))
        {
            return;
        }

        if (node.state != NodeState.Reachable)
        {
            return;
        }

        mapData.currentNodeId = node.id;
        SaveRuntimeState();

        if (verboseLogging)
        {
            Debug.Log($"NodeMap: Selected node {node.id} ({node.type}).");
        }

        if (config.autoLoadEncounterScene && !string.IsNullOrWhiteSpace(config.encounterSceneName))
        {
            SaveRuntimeState();
            SceneManager.LoadScene(config.encounterSceneName);
            return;
        }

        if (config.completeNodeImmediatelyInMapScene)
        {
            CompleteNode(node.id);
        }
        else
        {
            RefreshVisualState();
        }
    }

    public void CompleteNode(int nodeId)
    {
        if (!nodesById.TryGetValue(nodeId, out MapNodeData node))
        {
            return;
        }

        if (node.state == NodeState.Cleared)
        {
            return;
        }

        node.state = NodeState.Cleared;
        mapData.currentNodeId = node.id;

        for (int i = 0; i < node.nextNodeIds.Count; i++)
        {
            int childId = node.nextNodeIds[i];
            if (nodesById.TryGetValue(childId, out MapNodeData child) && child.state == NodeState.Locked)
            {
                child.state = NodeState.Reachable;
            }
        }

        SaveRuntimeState();
        RefreshVisualState();
    }

    public NodeMapData GetMapData()
    {
        return mapData;
    }

    private List<List<MapNodeData>> CreateLayers()
    {
        List<List<MapNodeData>> layers = new List<List<MapNodeData>>(config.layerCount);
        int nextNodeId = 0;

        for (int layerIndex = 0; layerIndex < config.layerCount; layerIndex++)
        {
            int nodeCount = GetNodeCountForLayer(layerIndex);
            List<MapNodeData> layerNodes = new List<MapNodeData>(nodeCount);
            float centerOffset = (nodeCount - 1) * 0.5f;

            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                float yPosition = (nodeIndex - centerOffset) * config.nodeSpacing;
                MapNodeData node = new MapNodeData
                {
                    id = nextNodeId++,
                    layer = layerIndex,
                    position = new Vector2(layerIndex * config.layerSpacing, yPosition)
                };

                mapData.nodes.Add(node);
                layerNodes.Add(node);
            }

            layers.Add(layerNodes);
        }

        return layers;
    }

    private int GetNodeCountForLayer(int layerIndex)
    {
        if (layerIndex == 0 || layerIndex == config.layerCount - 1)
        {
            return 1;
        }

        return random.Next(config.minNodesPerMiddleLayer, config.maxNodesPerMiddleLayer + 1);
    }

    private float GetJitter()
    {
        if (config.nodeJitter <= 0f)
        {
            return 0f;
        }

        return ((float)random.NextDouble() * 2f - 1f) * config.nodeJitter;
    }

    private void BuildGuaranteedPaths(List<List<MapNodeData>> layers)
    {
        int pathCount = Mathf.Max(1, config.guaranteedPathCount);

        for (int p = 0; p < pathCount; p++)
        {
            MapNodeData current = layers[0][0];
            for (int layerIndex = 1; layerIndex < layers.Count; layerIndex++)
            {
                List<MapNodeData> nextLayer = layers[layerIndex];
                MapNodeData next = nextLayer[random.Next(0, nextLayer.Count)];
                ConnectNodes(current, next, true);
                current = next;
            }
        }
    }

    private void EnsureNodeConnectivity(List<List<MapNodeData>> layers)
    {
        for (int layerIndex = 1; layerIndex < layers.Count - 1; layerIndex++)
        {
            List<MapNodeData> previousLayer = layers[layerIndex - 1];
            List<MapNodeData> currentLayer = layers[layerIndex];
            List<MapNodeData> nextLayer = layers[layerIndex + 1];

            for (int i = 0; i < currentLayer.Count; i++)
            {
                MapNodeData node = currentLayer[i];

                if (!HasIncomingConnection(previousLayer, node.id))
                {
                    MapNodeData parent = previousLayer[random.Next(0, previousLayer.Count)];
                    ConnectNodes(parent, node, true);
                }

                if (node.nextNodeIds.Count == 0)
                {
                    MapNodeData next = nextLayer[random.Next(0, nextLayer.Count)];
                    ConnectNodes(node, next, true);
                }
            }
        }
    }

    private bool HasIncomingConnection(List<MapNodeData> previousLayer, int targetNodeId)
    {
        for (int i = 0; i < previousLayer.Count; i++)
        {
            if (previousLayer[i].nextNodeIds.Contains(targetNodeId))
            {
                return true;
            }
        }

        return false;
    }

    private void AddExtraConnections(List<List<MapNodeData>> layers)
    {
        for (int layerIndex = 0; layerIndex < layers.Count - 1; layerIndex++)
        {
            List<MapNodeData> currentLayer = layers[layerIndex];
            List<MapNodeData> nextLayer = layers[layerIndex + 1];

            for (int i = 0; i < currentLayer.Count; i++)
            {
                MapNodeData from = currentLayer[i];

                for (int j = 0; j < nextLayer.Count; j++)
                {
                    if (from.nextNodeIds.Count >= config.maxOutgoingConnections)
                    {
                        break;
                    }

                    MapNodeData to = nextLayer[j];
                    if (from.nextNodeIds.Contains(to.id))
                    {
                        continue;
                    }

                    float distance = Mathf.Abs(from.position.y - to.position.y);
                    float distanceScale = Mathf.Max(config.nodeSpacing * Mathf.Max(1, nextLayer.Count - 1), 0.0001f);
                    float normalizedDistance = Mathf.Clamp01(distance / distanceScale);
                    float chance = Mathf.Lerp(config.extraConnectionChance, 0.05f, normalizedDistance);

                    if (random.NextDouble() <= chance)
                    {
                        ConnectNodes(from, to, false);
                    }
                }
            }
        }
    }

    private void ConnectNodes(MapNodeData from, MapNodeData to, bool ignoreOutgoingLimit)
    {
        if (from == null || to == null)
        {
            return;
        }

        if (to.layer != from.layer + 1)
        {
            return;
        }

        if (from.nextNodeIds.Contains(to.id))
        {
            return;
        }

        if (!ignoreOutgoingLimit && from.nextNodeIds.Count >= config.maxOutgoingConnections)
        {
            return;
        }

        from.nextNodeIds.Add(to.id);
    }

    // Avoid edge crossings by optimizing node positions using multi-pass ordering with crossing minimization
    private void OptimizeNodePositionsForEdgeCrossing(List<List<MapNodeData>> layers)
    {
        if (layers.Count <= 1) return;

        // Perform multiple aggressive passes with barycentric positioning and crossing minimization
        const int maxPasses = 10;
        const int crossingMinimizationIterations = 3;
        
        for (int pass = 0; pass < maxPasses; pass++)
        {
            bool isForwardPass = (pass % 2) == 0;
            
            if (isForwardPass)
            {
                // Left-to-right pass with barycentric positioning
                for (int layerIndex = 1; layerIndex < layers.Count; layerIndex++)
                {
                    OrderLayerByBarycenters(layers, layerIndex);
                    MinimizeCrossingsInLayer(layers, layerIndex, crossingMinimizationIterations);
                }
            }
            else
            {
                // Right-to-left pass with barycentric positioning
                for (int layerIndex = layers.Count - 2; layerIndex >= 0; layerIndex--)
                {
                    OrderLayerByBarycenters(layers, layerIndex);
                    MinimizeCrossingsInLayer(layers, layerIndex, crossingMinimizationIterations);
                }
            }
        }
        
        // Final normalization with jitter applied for visual interest
        NormalizeNodePositionsWithJitter(layers);
    }

    private void OrderLayerByBarycenters(List<List<MapNodeData>> layers, int layerIndex)
    {
        List<MapNodeData> currentLayer = layers[layerIndex];
        List<(MapNodeData node, float barycenter)> nodeBarycenters = new List<(MapNodeData, float)>(currentLayer.Count);

        foreach (MapNodeData node in currentLayer)
        {
            float barycenterY = GetNodeBarycenter(layers, node);
            nodeBarycenters.Add((node, barycenterY));
        }

        // Sort by barycenter position
        nodeBarycenters.Sort((a, b) => a.barycenter.CompareTo(b.barycenter));
        ReassignLayerPositions(currentLayer, nodeBarycenters);
    }

    private float GetNodeBarycenter(List<List<MapNodeData>> layers, MapNodeData node)
    {
        List<float> connectedYPositions = new List<float>();

        // Collect Y positions of all connected neighbors (both predecessors and successors)
        for (int neighborLayerIdx = 0; neighborLayerIdx < layers.Count; neighborLayerIdx++)
        {
            if (neighborLayerIdx == node.layer) continue;

            foreach (MapNodeData neighborNode in layers[neighborLayerIdx])
            {
                bool isConnected = false;
                
                if (neighborLayerIdx < node.layer && neighborNode.nextNodeIds.Contains(node.id))
                {
                    isConnected = true;
                }
                else if (neighborLayerIdx > node.layer && node.nextNodeIds.Contains(neighborNode.id))
                {
                    isConnected = true;
                }

                if (isConnected)
                {
                    connectedYPositions.Add(neighborNode.position.y);
                }
            }
        }

        // Return average of all connected neighbors' Y positions
        if (connectedYPositions.Count > 0)
        {
            float sum = 0f;
            foreach (float y in connectedYPositions)
            {
                sum += y;
            }
            return sum / connectedYPositions.Count;
        }

        return 0f;
    }

    private void MinimizeCrossingsInLayer(List<List<MapNodeData>> layers, int layerIndex, int iterations)
    {
        List<MapNodeData> layer = layers[layerIndex];
        
        for (int iter = 0; iter < iterations; iter++)
        {
            // Try adjacent swaps and accept if they reduce crossings
            for (int i = 0; i < layer.Count - 1; i++)
            {
                int crossingsBefore = CountLayerCrossings(layers, layerIndex);
                
                // Try swapping adjacent nodes
                MapNodeData temp = layer[i];
                layer[i] = layer[i + 1];
                layer[i + 1] = temp;
                
                int crossingsAfter = CountLayerCrossings(layers, layerIndex);
                
                // If crossings increased, swap back
                if (crossingsAfter > crossingsBefore)
                {
                    temp = layer[i];
                    layer[i] = layer[i + 1];
                    layer[i + 1] = temp;
                }
            }
        }

        // Reassign Y positions based on final ordering
        float centerOffset = (layer.Count - 1) * 0.5f;
        for (int i = 0; i < layer.Count; i++)
        {
            MapNodeData node = layer[i];
            float newYPosition = (i - centerOffset) * config.nodeSpacing;
            node.position = new Vector2(node.position.x, newYPosition);
        }
    }

    private int CountLayerCrossings(List<List<MapNodeData>> layers, int layerIndex)
    {
        List<MapNodeData> currentLayer = layers[layerIndex];
        int crossingCount = 0;

        // Count edge crossings involving this layer
        if (layerIndex > 0)
        {
            List<MapNodeData> prevLayer = layers[layerIndex - 1];
            
            // Check all pairs of edges from previous layer to current layer
            for (int i = 0; i < prevLayer.Count; i++)
            {
                for (int j = i + 1; j < prevLayer.Count; j++)
                {
                    MapNodeData node1 = prevLayer[i];
                    MapNodeData node2 = prevLayer[j];
                    
                    // Get indices in current layer
                    List<int> node1Targets = new List<int>();
                    List<int> node2Targets = new List<int>();
                    
                    foreach (int targetId in node1.nextNodeIds)
                    {
                        for (int k = 0; k < currentLayer.Count; k++)
                        {
                            if (currentLayer[k].id == targetId)
                            {
                                node1Targets.Add(k);
                                break;
                            }
                        }
                    }
                    
                    foreach (int targetId in node2.nextNodeIds)
                    {
                        for (int k = 0; k < currentLayer.Count; k++)
                        {
                            if (currentLayer[k].id == targetId)
                            {
                                node2Targets.Add(k);
                                break;
                            }
                        }
                    }
                    
                    // Check if edges cross (i < j but targets are reversed)
                    foreach (int idx1 in node1Targets)
                    {
                        foreach (int idx2 in node2Targets)
                        {
                            if ((i < j && idx1 > idx2) || (i > j && idx1 < idx2))
                            {
                                crossingCount++;
                            }
                        }
                    }
                }
            }
        }

        return crossingCount;
    }

    private void NormalizeNodePositionsWithJitter(List<List<MapNodeData>> layers)
    {
        // Ensure each layer is properly centered and apply jitter
        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            List<MapNodeData> layer = layers[layerIndex];
            if (layer.Count == 0) continue;

            // Sort by current Y position to maintain optimized ordering
            List<MapNodeData> sortedByY = new List<MapNodeData>(layer);
            sortedByY.Sort((a, b) => a.position.y.CompareTo(b.position.y));

            // Reassign with proper spacing centered on layer, then apply jitter
            float centerOffset = (layer.Count - 1) * 0.5f;
            for (int i = 0; i < sortedByY.Count; i++)
            {
                MapNodeData node = sortedByY[i];
                float baseYPosition = (i - centerOffset) * config.nodeSpacing;
                float jitter = GetJitter();
                float finalYPosition = baseYPosition + jitter;
                node.position = new Vector2(node.position.x, finalYPosition);
            }
        }
    }

    private void ReassignLayerPositions(List<MapNodeData> layer, List<(MapNodeData node, float sortKey)> sortedNodes)
    {
        float centerOffset = (layer.Count - 1) * 0.5f;
        
        for (int i = 0; i < sortedNodes.Count; i++)
        {
            MapNodeData node = sortedNodes[i].node;
            float newYPosition = (i - centerOffset) * config.nodeSpacing;
            node.position = new Vector2(node.position.x, newYPosition);
        }
    }

    private void AssignNodeTypes(List<List<MapNodeData>> layers)
    {
        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            List<MapNodeData> layer = layers[layerIndex];

            for (int i = 0; i < layer.Count; i++)
            {
                MapNodeData node = layer[i];

                if (layerIndex == 0)
                {
                    node.type = MapNodeType.Start;
                    continue;
                }

                if (layerIndex == layers.Count - 1)
                {
                    node.type = MapNodeType.Boss;
                    continue;
                }

                node.type = PickEncounterType(layerIndex, layers.Count);
            }
        }
    }

    private MapNodeType PickEncounterType(int layerIndex, int totalLayers)
    {
        int totalWeight = 0;
        List<NodeTypeWeight> weights = config.nodeTypeWeights;

        for (int i = 0; i < weights.Count; i++)
        {
            NodeTypeWeight entry = weights[i];
            if (!IsEncounterType(entry.type) || entry.weight <= 0)
            {
                continue;
            }

            totalWeight += GetDepthAdjustedWeight(entry, layerIndex, totalLayers);
        }

        if (totalWeight <= 0)
        {
            return MapNodeType.Battle;
        }

        int roll = random.Next(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < weights.Count; i++)
        {
            NodeTypeWeight entry = weights[i];
            if (!IsEncounterType(entry.type) || entry.weight <= 0)
            {
                continue;
            }

            cumulative += GetDepthAdjustedWeight(entry, layerIndex, totalLayers);
            if (roll < cumulative)
            {
                return entry.type;
            }
        }

        return MapNodeType.Battle;
    }

    private int GetDepthAdjustedWeight(NodeTypeWeight entry, int layerIndex, int totalLayers)
    {
        int value = entry.weight;

        if (entry.type == MapNodeType.Elite)
        {
            if (layerIndex <= 1)
            {
                value = Mathf.Max(1, value / 3);
            }

            if (layerIndex >= totalLayers - 3)
            {
                value = Mathf.RoundToInt(value * 1.5f);
            }
        }

        return Mathf.Max(0, value);
    }

    private bool IsEncounterType(MapNodeType type)
    {
        return type != MapNodeType.Start && type != MapNodeType.Boss;
    }

    private void InitializeNodeStates(List<List<MapNodeData>> layers)
    {
        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            mapData.nodes[i].state = NodeState.Locked;
        }

        MapNodeData startNode = layers[0][0];
        startNode.state = NodeState.Cleared;
        mapData.currentNodeId = startNode.id;

        for (int i = 0; i < startNode.nextNodeIds.Count; i++)
        {
            MapNodeData next = mapData.FindNodeById(startNode.nextNodeIds[i]);
            if (next != null)
            {
                next.state = NodeState.Reachable;
            }
        }
    }

    private void BuildLookup()
    {
        nodesById.Clear();

        if (mapData == null || mapData.nodes == null)
        {
            return;
        }

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            MapNodeData node = mapData.nodes[i];
            nodesById[node.id] = node;
        }
    }

    private void RebuildVisuals()
    {
        EnsureMapRoot();
        ClearMapRoot();

        viewsById.Clear();
        connectionVisuals.Clear();

        if (mapData == null || mapData.nodes == null)
        {
            return;
        }

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            MapNodeData node = mapData.nodes[i];
            MapNodeView view = CreateNodeView(node);
            viewsById[node.id] = view;
        }

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            MapNodeData from = mapData.nodes[i];
            for (int j = 0; j < from.nextNodeIds.Count; j++)
            {
                if (nodesById.TryGetValue(from.nextNodeIds[j], out MapNodeData to))
                {
                    CreateConnectionVisual(from, to);
                }
            }
        }

        RefreshVisualState();
    }

    private void EnsureMapRoot()
    {
        if (mapRoot != null)
        {
            return;
        }

        Transform existing = transform.Find("GeneratedNodeMap");
        if (existing != null)
        {
            mapRoot = existing;
            return;
        }

        GameObject root = new GameObject("GeneratedNodeMap");
        root.transform.SetParent(transform, false);
        mapRoot = root.transform;
    }

    private void ClearMapRoot()
    {
        if (mapRoot == null)
        {
            return;
        }

        for (int i = mapRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = mapRoot.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private MapNodeView CreateNodeView(MapNodeData node)
    {
        MapNodeView view;
        Sprite nodeSprite = ResolveNodeSprite();

        if (nodeViewPrefab != null)
        {
            view = Instantiate(nodeViewPrefab, mapRoot);
        }
        else
        {
            GameObject spriteNode = new GameObject("MapNode");
            spriteNode.transform.SetParent(mapRoot, false);

            SpriteRenderer spriteRenderer = spriteNode.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = nodeSprite;
            spriteRenderer.sortingOrder = nodeSortingOrder;

            spriteNode.AddComponent<CircleCollider2D>();
            view = spriteNode.AddComponent<MapNodeView>();
        }

        view.transform.localPosition = new Vector3(node.position.x, node.position.y, 0f);
        view.transform.localScale = Vector3.one * config.nodeScale;
        view.Setup(this, node, nodeSprite, nodeSortingOrder);

        return view;
    }

    private void CreateConnectionVisual(MapNodeData from, MapNodeData to)
    {
        GameObject lineObject = new GameObject($"Edge_{from.id}_{to.id}");
        lineObject.transform.SetParent(mapRoot, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.numCapVertices = 4;
        line.startWidth = config.lineWidth;
        line.endWidth = config.lineWidth;
        line.sortingOrder = lineSortingOrder;
        line.material = ResolveLineMaterial();
        line.SetPosition(0, new Vector3(from.position.x, from.position.y, 0f));
        line.SetPosition(1, new Vector3(to.position.x, to.position.y, 0f));

        connectionVisuals.Add(new ConnectionVisual
        {
            fromId = from.id,
            toId = to.id,
            line = line
        });
    }

    private Material ResolveLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        if (runtimeLineMaterial != null)
        {
            return runtimeLineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        runtimeLineMaterial = new Material(shader);
        return runtimeLineMaterial;
    }

    private void RefreshVisualState()
    {
        foreach (KeyValuePair<int, MapNodeView> pair in viewsById)
        {
            if (nodesById.TryGetValue(pair.Key, out MapNodeData node))
            {
                pair.Value.SetState(node.state);
            }
        }

        for (int i = 0; i < connectionVisuals.Count; i++)
        {
            ConnectionVisual visual = connectionVisuals[i];
            if (!nodesById.TryGetValue(visual.fromId, out MapNodeData fromNode) ||
                !nodesById.TryGetValue(visual.toId, out MapNodeData toNode) ||
                visual.line == null)
            {
                continue;
            }

            bool isActive = fromNode.state == NodeState.Cleared &&
                            (toNode.state == NodeState.Reachable || toNode.state == NodeState.Cleared);
            Color color = isActive ? config.activeConnectionColor : config.inactiveConnectionColor;
            visual.line.startColor = color;
            visual.line.endColor = color;
        }

        RefreshPlayerMarker();
    }

    private Sprite ResolveNodeSprite()
    {
        if (defaultNodeSprite != null)
        {
            return defaultNodeSprite;
        }

        return GetOrCreateCircleSprite();
    }

    private Sprite ResolvePlayerMarkerSprite()
    {
        if (playerMarkerSprite != null)
        {
            return playerMarkerSprite;
        }

        return GetOrCreateCircleSprite();
    }

    private Sprite GetOrCreateCircleSprite()
    {
        if (generatedCircleSprite != null)
        {
            return generatedCircleSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "GeneratedNodeCircleTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        generatedCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        generatedCircleSprite.name = "GeneratedMapCircleSprite";

        return generatedCircleSprite;
    }

    private void EnsurePlayerMarker()
    {
        if (mapRoot == null)
        {
            return;
        }

        if (playerMarkerObject != null && playerMarkerRenderer != null)
        {
            return;
        }

        playerMarkerObject = new GameObject("PlayerMarker");
        playerMarkerObject.transform.SetParent(mapRoot, false);
        playerMarkerRenderer = playerMarkerObject.AddComponent<SpriteRenderer>();
    }

    private void RefreshPlayerMarker()
    {
        if (mapData == null || !nodesById.TryGetValue(mapData.currentNodeId, out MapNodeData currentNode))
        {
            if (playerMarkerRenderer != null)
            {
                playerMarkerRenderer.enabled = false;
            }

            return;
        }

        EnsurePlayerMarker();
        if (playerMarkerRenderer == null)
        {
            return;
        }

        playerMarkerRenderer.enabled = true;
        playerMarkerRenderer.sprite = ResolvePlayerMarkerSprite();
        playerMarkerRenderer.color = playerMarkerColor;
        playerMarkerRenderer.sortingOrder = playerMarkerSortingOrder;

        playerMarkerObject.transform.localScale = Vector3.one * Mathf.Max(0.05f, playerMarkerScale);
        playerMarkerObject.transform.localPosition =
            new Vector3(currentNode.position.x, currentNode.position.y, 0f) + playerMarkerOffset;
    }

    private void SaveRuntimeState()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        MapRunState.Instance.SaveMap(mapData);
    }

    private Camera ResolveInputCamera()
    {
        if (inputCamera != null)
        {
            return inputCamera;
        }

        return Camera.main;
    }

    private MapNodeView RaycastNodeView(Camera cameraToUse, Vector2 screenPosition)
    {
        Vector3 worldPoint = ScreenToWorldOnMapPlane(cameraToUse, screenPosition);
        Collider2D hit2D = Physics2D.OverlapPoint(new Vector2(worldPoint.x, worldPoint.y), nodeClickMask.value);
        if (hit2D != null)
        {
            return hit2D.GetComponentInParent<MapNodeView>();
        }

        Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit3D, 1000f, nodeClickMask.value))
        {
            return hit3D.collider.GetComponentInParent<MapNodeView>();
        }

        return null;
    }

    private Vector3 ScreenToWorldOnMapPlane(Camera cameraToUse, Vector2 screenPosition)
    {
        float targetZ = mapRoot != null ? mapRoot.position.z : transform.position.z;
        float depth = Mathf.Abs(targetZ - cameraToUse.transform.position.z);

        if (cameraToUse.orthographic)
        {
            depth = cameraToUse.nearClipPlane;
        }

        return cameraToUse.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
    }

    private bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            if (EventSystem.current.IsPointerOverGameObject(touchId))
            {
                return true;
            }
        }
#endif

        return EventSystem.current.IsPointerOverGameObject();
    }

    private bool TryGetPointerDownThisFrame(out Vector2 screenPosition)
    {
        screenPosition = default;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            screenPosition = Input.GetTouch(0).position;
            return true;
        }
#endif

        return false;
    }
}
