using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class NodeMap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapConfig config;
    [SerializeField] private SceneChanger sceneChanger;
    [SerializeField] private Canvas mapCanvas;
    [SerializeField] private RectTransform mapRoot;
    [SerializeField] private MapNodeView nodeViewPrefab;

    [Header("Layering")]
    [SerializeField] private string mapLayerName = "map";

    [Header("UI Visuals")]
    [SerializeField] private Sprite defaultNodeSprite;
    [SerializeField] private Sprite startNodeSprite;
    [SerializeField] private Sprite battleNodeSprite;
    [SerializeField] private Sprite eliteNodeSprite;
    [SerializeField] private Sprite bossNodeSprite;
    [SerializeField] private float mapUnitsToPixels = 120f;
    [SerializeField] private float nodeUiSize = 96f;
    [SerializeField, Min(0.05f)] private float bossNodeScaleMultiplier = 1.35f;
    //[SerializeField] private string enemyNameTooltipText = "???";
    //[SerializeField] private string enemyHealthTooltipText = "???";
    //[SerializeField] private string battlePayoutTooltipText = "???";
    [SerializeField] private MapConnectionVisualSettings connectionVisualSettings = new MapConnectionVisualSettings
    {
        lineWidth = 10f,
        dottedCycleLength = 28f,
        dottedFillRatio = 0.45f,
        activeScrollSpeed = 0.75f
    };
    [SerializeField] private Sprite playerMarkerSprite;
    [SerializeField] private Color playerMarkerColor = new Color(0.2f, 0.95f, 1f, 1f);
    [SerializeField] private Vector3 playerMarkerOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private float playerMarkerScale = 0.55f;
    [SerializeField, Min(0f)] private float playerMarkerBobAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float playerMarkerBobFrequency = 1.6f;
    [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private bool autoCreateCanvasWhenMissing = true;
    [SerializeField] private float mapDragThreshold = 5f;
    [SerializeField] private float panOvershoot = 200f;

    [Header("Runtime")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool loadFromRunState = true;
    [SerializeField] private int fixedSeed = -1;
    [SerializeField] private bool animateRecentlyDefeatedNodeOnMapEntry = true;
    [SerializeField] private bool recentlyDefeatedNodeUsesShadedVariant = true;
    [SerializeField, Min(0f)] private float mapEntryDefeatAnimationDelay = 0f;
    [SerializeField] private bool useSceneChangerTransitionTimeForMapEntryDelay = true;

    private NodeMapData mapData;
    private System.Random random;
    private Sprite generatedCircleSprite;
    private GameObject playerMarkerObject;
    private Image playerMarkerImage;
    private bool hasWarnedNonUiNodePrefab;
    private int cachedMapLayer = int.MinValue;
    private bool hasWarnedMissingMapLayer;
    private bool hasWarnedMissingNodeDefeatAnimator;
    private int pendingMapEntryDefeatAnimationNodeId = -1;
    private Coroutine pendingMapEntryDefeatAnimationCoroutine;

    private readonly Dictionary<int, MapNodeData> nodesById = new Dictionary<int, MapNodeData>();
    private readonly Dictionary<int, MapNodeView> viewsById = new Dictionary<int, MapNodeView>();
    private readonly List<ConnectionVisual> connectionVisuals = new List<ConnectionVisual>();

    // Drag panning state
    private Vector2 dragStartMousePosition;
    private Vector2 lastDragMousePosition;
    private bool isDragging;
    private bool dragThresholdMet;
    private float panMinX = float.NegativeInfinity;
    private float panMaxX = float.PositiveInfinity;

    private const float MinUiElementSize = 4f;
    private const float MinElementScale = 0.05f;
    private const int EdgeCrossingOptimizationPasses = 8;

    // Represents a visual connection between two nodes
    private struct ConnectionVisual
    {
        public int fromId;
        public int toId;
        public MapConnectionVisual view;
    }

    private void Update()
    {
        if (mapRoot == null || mapCanvas == null)
            return;

#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;
        bool pressedThisFrame  = mouse.leftButton.wasPressedThisFrame;
        bool releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
        bool isPressed         = mouse.leftButton.isPressed;
        Vector2 mousePos       = mouse.position.ReadValue();
#else
        bool pressedThisFrame  = Input.GetMouseButtonDown(0);
        bool releasedThisFrame = Input.GetMouseButtonUp(0);
        bool isPressed         = Input.GetMouseButton(0);
        Vector2 mousePos       = Input.mousePosition;
#endif

        if (pressedThisFrame)
        {
            dragStartMousePosition = mousePos;
            lastDragMousePosition = mousePos;
            isDragging = true;
            dragThresholdMet = false;
        }

        if (releasedThisFrame)
        {
            isDragging = false;
            dragThresholdMet = false;
        }

        if (isDragging && isPressed)
        {
            if (!dragThresholdMet)
            {
                float threshold = Mathf.Max(0f, mapDragThreshold);
                dragThresholdMet = (mousePos - dragStartMousePosition).sqrMagnitude > threshold * threshold;
            }

            if (dragThresholdMet)
            {
                Vector2 delta = mousePos - lastDragMousePosition;
                SetMapRootX(mapRoot.anchoredPosition.x + delta.x / GetCanvasScaleFactor());
            }

            lastDragMousePosition = mousePos;
        }

        RefreshPlayerMarker();
    }

    private void ComputePanBounds()
    {
        if (mapCanvas == null || mapRoot == null || mapData == null || mapData.nodes == null || mapData.nodes.Count == 0)
        {
            panMinX = float.NegativeInfinity;
            panMaxX = float.PositiveInfinity;
            return;
        }

        float nodeMapMinX = float.PositiveInfinity;
        float nodeMapMaxX = float.NegativeInfinity;
        float mapScale = GetMapScale();

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            float x = mapData.nodes[i].position.x * mapScale;
            if (x < nodeMapMinX) nodeMapMinX = x;
            if (x > nodeMapMaxX) nodeMapMaxX = x;
        }

        float canvasWidth = GetCanvasWidth();
        float baseMargin = ResolveLargestNodeUiSize() * 0.5f;
        float halfCanvas = canvasWidth * 0.5f;

        panMinX = -halfCanvas + baseMargin - panOvershoot - nodeMapMaxX;
        panMaxX = halfCanvas - baseMargin + panOvershoot - nodeMapMinX;

        if (panMinX > panMaxX)
        {
            float center = -(nodeMapMinX + nodeMapMaxX) * 0.5f;
            panMinX = center;
            panMaxX = center;
        }
    }

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("NodeMap: MapConfig reference is missing.");
            return;
        }

        EnsureEventSystem();
        ResolveSceneChanger();

        if (!generateOnStart)
            return;

        if (TryLoadMapFromRunState())
            return;

        GenerateNewMap();
    }

    private void OnDisable()
    {
        StopPendingMapEntryDefeatAnimation();
    }

    private void OnValidate()
    {
        cachedMapLayer = int.MinValue;
        hasWarnedMissingMapLayer = false;
        hasWarnedNonUiNodePrefab = false;
        hasWarnedMissingNodeDefeatAnimator = false;
    }

    // Generates a new map using the current configuration and a random seed
    [ContextMenu("Generate New Map")]
    public void GenerateNewMap()
    {
        int seed = fixedSeed >= 0 ? fixedSeed : UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        GenerateNewMap(seed);
    }

    // Generates a new map using a specific seed instead
    public void GenerateNewMap(int seed)
    {
        if (config == null)
        {
            Debug.LogError("NodeMap: Cannot generate map because config is missing.");
            return;
        }

        pendingMapEntryDefeatAnimationNodeId = -1;
        StopPendingMapEntryDefeatAnimation();

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

        // On a loop, queue the start node's defeat animation so it plays on map entry
        if (animateRecentlyDefeatedNodeOnMapEntry && MapRunState.Instance.LoopCount > 0
            && layers.Count > 0 && layers[0].Count > 0)
        {
            pendingMapEntryDefeatAnimationNodeId = layers[0][0].id;
        }

        BuildLookup();
        RebuildVisuals();
        ComputePanBounds();
        CenterOnCurrentNode();
        TryQueuePendingMapEntryDefeatAnimation();
        SaveRuntimeState();
    }

    // Completes the current node in the map
    [ContextMenu("Complete Current Node")]
    public void CompleteCurrentNode()
    {
        if (mapData == null || mapData.currentNodeId < 0)
        {
            return;
        }

        CompleteNode(mapData.currentNodeId);
    }

    // Called when a node is clicked by the player
    public void OnNodeClicked(int nodeId)
    {
        if (mapData == null || !nodesById.TryGetValue(nodeId, out MapNodeData node))
        {
            return;
        }

        if (TryGetCurrentNode(out MapNodeData currentNode) && node.layer <= currentNode.layer)
        {
            return;
        }

        if (node.state != NodeState.Reachable)
        {
            return;
        }

        mapData.currentNodeId = node.id;
        EnemyManager.Instance.enemyInfo = node.enemyInfo;
        SaveRuntimeState();

        if (TryLoadEncounterScene())
        {
            return;
        }

        if (config.completeNodeImmediatelyInMapScene)
        {
            CompleteNode(node.id);
        }
        else
        {
            UpdateNodeStates();
            RefreshVisualState();
        }
    }

    // Completes the specified node in the map
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

        UpdateNodeStates();
        SaveRuntimeState();
        RefreshVisualState();
    }

    // Updates the states of all nodes based on the current node
    private void UpdateNodeStates()
    {
        if (mapData == null || mapData.nodes == null || !TryGetCurrentNode(out MapNodeData currentNode))
        {
            return;
        }

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            MapNodeData node = mapData.nodes[i];
            if (node.id != currentNode.id && node.layer <= currentNode.layer && node.state != NodeState.Cleared)
            {
                node.state = NodeState.Locked;
            }
        }
    }

    public NodeMapData GetMapData() => mapData;

    public bool IsCurrentNode(int id)
    {
        return mapData != null && mapData.currentNodeId == id;
    }

    // Creates the layers of nodes for the map
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

    // Determines the number of nodes for a given layer
    private int GetNodeCountForLayer(int layerIndex)
    {
        if (layerIndex == 0 || layerIndex == config.layerCount - 1)
        {
            return 1;
        }

        return random.Next(config.minNodesPerMiddleLayer, config.maxNodesPerMiddleLayer + 1);
    }

    // Builds the guaranteed paths through the map
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

    // Ensures that all nodes have at least one incoming and one outgoing connection
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

    // Checks to make sure that this node has an incoming connection
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
            float distanceScale = Mathf.Max(config.nodeSpacing * Mathf.Max(1, nextLayer.Count - 1), 0.0001f);

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

    // Connects two nodes to one another
    private void ConnectNodes(MapNodeData from, MapNodeData to, bool ignoreOutgoingLimit)
    {
        if (from == null || to == null || to.layer != from.layer + 1 || 
            from.nextNodeIds.Contains(to.id) || 
            (!ignoreOutgoingLimit && from.nextNodeIds.Count >= config.maxOutgoingConnections))
            return;

        from.nextNodeIds.Add(to.id);
    }

    // Optimize node positions to minimize edge crossings using barycenter method
    private void OptimizeNodePositionsForEdgeCrossing(List<List<MapNodeData>> layers)
    {
        if (layers.Count <= 1) return;

        // Multiple passes alternating between forward and backward
        for (int pass = 0; pass < EdgeCrossingOptimizationPasses; pass++)
        {
            if (pass % 2 == 0)
            {
                for (int i = 1; i < layers.Count; i++)
                    OrderLayerByNeighbors(layers, i, true);
            }
            else
            {
                for (int i = layers.Count - 2; i >= 0; i--)
                    OrderLayerByNeighbors(layers, i, false);
            }
        }

        // Final position normalization
        for (int i = 0; i < layers.Count; i++)
            UpdateLayerPositions(layers[i]);
    }

    private void OrderLayerByNeighbors(List<List<MapNodeData>> layers, int layerIndex, bool byPredecessors)
    {
        List<MapNodeData> currentLayer = layers[layerIndex];
        List<MapNodeData> neighborLayer = byPredecessors ? layers[layerIndex - 1] : layers[layerIndex + 1];
        List<(MapNodeData node, float barycenter)> nodePositions = new List<(MapNodeData, float)>(currentLayer.Count);

        for (int nodeIndex = 0; nodeIndex < currentLayer.Count; nodeIndex++)
        {
            MapNodeData node = currentLayer[nodeIndex];
            float sum = 0f;
            int count = 0;

            if (byPredecessors)
            {
                for (int i = 0; i < neighborLayer.Count; i++)
                {
                    if (neighborLayer[i].nextNodeIds.Contains(node.id))
                    {
                        sum += i;
                        count++;
                    }
                }
            }
            else if (node.nextNodeIds.Count > 0)
            {
                for (int i = 0; i < neighborLayer.Count; i++)
                {
                    if (node.nextNodeIds.Contains(neighborLayer[i].id))
                    {
                        sum += i;
                        count++;
                    }
                }
            }

            float fallback = byPredecessors ? nodeIndex : node.position.y;
            nodePositions.Add((node, count > 0 ? sum / count : fallback));
        }

        nodePositions.Sort((a, b) => a.barycenter.CompareTo(b.barycenter));
        for (int i = 0; i < nodePositions.Count; i++)
        {
            currentLayer[i] = nodePositions[i].node;
        }

        UpdateLayerPositions(currentLayer);
    }

    // Changes the position of the layers depending on configuration values
    private void UpdateLayerPositions(List<MapNodeData> layer)
    {
        if (layer.Count == 0) return;

        float centerOffset = (layer.Count - 1) * 0.5f;
        for (int i = 0; i < layer.Count; i++)
        {
            MapNodeData node = layer[i];
            float yPosition = (i - centerOffset) * config.nodeSpacing;
            node.position = new Vector2(node.position.x, yPosition);
        }
    }

    // Assigns node types based on configuration weights
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
                    node.enemyInfo = new EnemyInformation(node.type, GetProjectedEnemiesDefeatedForLayer(layerIndex));
                    continue;
                }

                node.type = PickEncounterType(layerIndex, layers.Count);
                //HERE IS WHERE WE ASSIGN THE THING
                node.enemyInfo = new EnemyInformation(node.type, GetProjectedEnemiesDefeatedForLayer(layerIndex));
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

    private bool IsEncounterType(MapNodeType type) => type != MapNodeType.Start && type != MapNodeType.Boss;

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
        if (mapRoot == null)
        {
            return;
        }

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
        if (mapCanvas == null)
        {
            mapCanvas = GetComponentInParent<Canvas>();
        }

        if (mapCanvas == null && autoCreateCanvasWhenMissing)
        {
            mapCanvas = CreateCanvas();
        }

        if (mapCanvas != null)
        {
            mapCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            if (mapCanvas.worldCamera == null)
                mapCanvas.worldCamera = Camera.main;
        }

        if (mapRoot == null)
        {
            Transform parent = mapCanvas != null ? mapCanvas.transform : transform;
            mapRoot = parent.Find("GeneratedNodeMap") as RectTransform;
            if (mapRoot == null)
            {
                GameObject root = new GameObject("GeneratedNodeMap", typeof(RectTransform));
                root.transform.SetParent(parent, false);
                mapRoot = root.GetComponent<RectTransform>();
            }
        }

        if (mapCanvas != null && mapRoot != null && mapRoot.parent != mapCanvas.transform)
        {
            mapRoot.SetParent(mapCanvas.transform, false);
        }

        if (mapRoot == null)
        {
            Debug.LogError("NodeMap: Failed to create/find a UI map root.");
            return;
        }

        ConfigureCenteredRect(mapRoot);
        mapRoot.anchoredPosition = Vector2.zero;
        mapRoot.localScale = Vector3.one;

        ApplyMapLayer(mapRoot.gameObject);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "NodeMapCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = canvasReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
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
        Sprite nodeSprite = ResolveNodeSprite(node.type);

        if (nodeViewPrefab != null && nodeViewPrefab.GetComponent<RectTransform>() != null)
        {
            view = Instantiate(nodeViewPrefab, mapRoot);
        }
        else
        {
            if (nodeViewPrefab != null && !hasWarnedNonUiNodePrefab)
            {
                hasWarnedNonUiNodePrefab = true;
                Debug.LogWarning("NodeMap: Node view prefab is not a UI prefab (missing RectTransform). Using generated UI nodes.");
            }

            GameObject uiNode = new GameObject("MapNode", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapNodeView));
            uiNode.transform.SetParent(mapRoot, false);

            Image image = uiNode.GetComponent<Image>();
            image.sprite = nodeSprite;
            image.preserveAspect = true;
            image.raycastTarget = true;

            Button button = uiNode.GetComponent<Button>();
            button.transition = Selectable.Transition.None;

            view = uiNode.GetComponent<MapNodeView>();
        }

        ApplyMapLayer(view.gameObject, recursive: true);

        RectTransform nodeRect = view.GetComponent<RectTransform>();
        if (nodeRect != null)
        {
            ConfigureCenteredRect(nodeRect);
            float size = ResolveNodeUiSize(node.type);
            nodeRect.sizeDelta = new Vector2(size, size);
            nodeRect.anchoredPosition = MapToUiPosition(node.position);
            nodeRect.localRotation = Quaternion.identity;
            nodeRect.localScale = Vector3.one;
        }

        view.Setup(this, node, nodeSprite);
        view.SetEnemyNameText(node.enemyInfo.EnemyName);
        view.SetEnemyHealthText(node.enemyInfo.getHealthString());
        view.SetBattlePayoutText(node.enemyInfo.getPayoutString());

        return view;
    }

    private void CreateConnectionVisual(MapNodeData from, MapNodeData to)
    {
        GameObject lineObject = new GameObject($"Edge_{from.id}_{to.id}", typeof(RectTransform), typeof(RawImage), typeof(MapConnectionVisual));
        lineObject.transform.SetParent(mapRoot, false);
        lineObject.transform.SetAsFirstSibling();
        ApplyMapLayer(lineObject);

        Vector2 fromPosition = MapToUiPosition(from.position);
        Vector2 toPosition = MapToUiPosition(to.position);

        MapConnectionVisual lineView = lineObject.GetComponent<MapConnectionVisual>();
        lineView.Setup(GetConnectionVisualSettings());
        lineView.SetConnection(fromPosition, toPosition, config.inactiveConnectionColor, shouldScroll: false);

        connectionVisuals.Add(new ConnectionVisual
        {
            fromId = from.id,
            toId = to.id,
            view = lineView
        });
    }

    private void RefreshVisualState()
    {
        foreach (KeyValuePair<int, MapNodeView> pair in viewsById)
        {
            if (pair.Value == null || !nodesById.TryGetValue(pair.Key, out MapNodeData node))
            {
                continue;
            }

            pair.Value.SetState(node.state);
            ApplyNodeDefeatVisualState(node, pair.Value);
        }

        for (int i = 0; i < connectionVisuals.Count; i++)
        {
            ConnectionVisual visual = connectionVisuals[i];
            if (!nodesById.TryGetValue(visual.fromId, out MapNodeData fromNode) ||
                !nodesById.TryGetValue(visual.toId, out MapNodeData toNode) ||
                visual.view == null)
            {
                continue;
            }

            bool isActive = fromNode.state == NodeState.Cleared &&
                            (toNode.state == NodeState.Reachable || toNode.state == NodeState.Cleared);
            bool shouldScroll = fromNode.state == NodeState.Cleared && toNode.state == NodeState.Reachable;
            Color color = isActive ? config.activeConnectionColor : config.inactiveConnectionColor;
            Vector2 fromPosition = MapToUiPosition(fromNode.position);
            Vector2 toPosition = MapToUiPosition(toNode.position);
            visual.view.SetConnection(fromPosition, toPosition, color, shouldScroll);
        }

        RefreshPlayerMarker();
    }

    private void ApplyNodeDefeatVisualState(MapNodeData node, MapNodeView view)
    {
        if (node == null || view == null)
        {
            return;
        }

        if (ShouldShowMapDeadVisual(node))
        {
            if (!view.SetEnemyAlreadyDefeatedVisual() && !hasWarnedMissingNodeDefeatAnimator)
            {
                hasWarnedMissingNodeDefeatAnimator = true;
                Debug.LogWarning("NodeMap: Cleared map enemies need an Animator with MapDead/EnemyDead/Shaded bools to display defeat visuals.");
            }

            return;
        }

        view.SetEnemyAliveVisual();
    }

    private bool ShouldShowMapDeadVisual(MapNodeData node)
    {
        if ((!IsEnemyNode(node) && !IsLoopStartNode(node)) || node.state != NodeState.Cleared)
        {
            return false;
        }

        return node.id != pendingMapEntryDefeatAnimationNodeId;
    }

    private static bool IsEnemyNode(MapNodeData node)
    {
        return node != null && node.type != MapNodeType.Start;
    }

    private static bool IsLoopStartNode(MapNodeData node)
    {
        return node != null && node.type == MapNodeType.Start
            && MapRunState.Instance != null && MapRunState.Instance.LoopCount > 0;
    }

    private Sprite ResolveNodeSprite(MapNodeType type)
    {
        // On a loop, the start node displays the boss sprite (it represents the defeated prior boss)
        if (type == MapNodeType.Start && MapRunState.Instance.LoopCount > 0)
        {
            return bossNodeSprite != null ? bossNodeSprite : (defaultNodeSprite != null ? defaultNodeSprite : GetOrCreateCircleSprite());
        }

        Sprite sprite = type switch
        {
            MapNodeType.Start => startNodeSprite,
            MapNodeType.Battle => battleNodeSprite,
            MapNodeType.Elite => eliteNodeSprite,
            MapNodeType.Boss => bossNodeSprite,
            _ => defaultNodeSprite
        };

        if (sprite != null)
        {
            return sprite;
        }

        return defaultNodeSprite != null ? defaultNodeSprite : GetOrCreateCircleSprite();
    }

    private Sprite ResolvePlayerMarkerSprite() => playerMarkerSprite != null ? playerMarkerSprite : GetOrCreateCircleSprite();

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

        float center = (size - 1) * 0.5f;
        float radius = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            float dy = y - center;
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
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
        if (mapRoot == null || (playerMarkerObject != null && playerMarkerImage != null)) return;

        playerMarkerObject = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        playerMarkerObject.transform.SetParent(mapRoot, false);
        ApplyMapLayer(playerMarkerObject);

        playerMarkerImage = playerMarkerObject.GetComponent<Image>();
        playerMarkerImage.raycastTarget = false;

        RectTransform markerRect = playerMarkerObject.GetComponent<RectTransform>();
        ConfigureCenteredRect(markerRect);
    }

    private float GetMapScale() => Mathf.Max(1f, mapUnitsToPixels);

    private float GetCanvasScaleFactor() => mapCanvas != null && mapCanvas.scaleFactor > 0f ? mapCanvas.scaleFactor : 1f;

    private float GetCanvasWidth()
    {
        if (mapCanvas == null)
        {
            return canvasReferenceResolution.x;
        }

        Canvas.ForceUpdateCanvases();
        RectTransform canvasRect = mapCanvas.GetComponent<RectTransform>();
        float width = canvasRect.rect.width;
        return width > 0f ? width : canvasReferenceResolution.x;
    }

    private float ResolveUiElementSize(float scaleMultiplier) =>
        Mathf.Max(MinUiElementSize, nodeUiSize * Mathf.Max(MinElementScale, scaleMultiplier));

    private float ResolveNodeUiSize(MapNodeType type)
    {
        float baseScale = config != null ? config.nodeScale : 1f;
        return ResolveUiElementSize(baseScale * GetNodeScaleMultiplier(type));
    }

    private float ResolveLargestNodeUiSize()
    {
        float baseScale = config != null ? config.nodeScale : 1f;
        float largestMultiplier = Mathf.Max(1f, GetNodeScaleMultiplier(MapNodeType.Boss));
        return ResolveUiElementSize(baseScale * largestMultiplier);
    }

    private float GetNodeScaleMultiplier(MapNodeType type)
    {
        if (type == MapNodeType.Boss)
        {
            return Mathf.Max(MinElementScale, bossNodeScaleMultiplier);
        }

        return 1f;
    }

    private float ClampPanX(float value) => Mathf.Clamp(value, panMinX, panMaxX);

    private void SetMapRootX(float x)
    {
        if (mapRoot == null)
        {
            return;
        }

        mapRoot.anchoredPosition = new Vector2(ClampPanX(x), mapRoot.anchoredPosition.y);
    }

    private Vector2 MapToUiPosition(Vector2 mapPosition) => mapPosition * GetMapScale();

    private MapConnectionVisualSettings GetConnectionVisualSettings()
    {
        MapConnectionVisualSettings settings = connectionVisualSettings;
        if (settings.lineWidth <= 0f)
        {
            settings.lineWidth = 10f;
        }

        if (settings.dottedCycleLength <= 0f)
        {
            settings.dottedCycleLength = 28f;
        }

        if (settings.dottedFillRatio <= 0f)
        {
            settings.dottedFillRatio = 0.45f;
        }

        settings.dottedFillRatio = Mathf.Clamp(settings.dottedFillRatio, 0.05f, 0.95f);
        settings.activeScrollSpeed = Mathf.Max(0f, settings.activeScrollSpeed);
        return settings;
    }

    private static void ConfigureCenteredRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void ApplyMapLayer(GameObject target, bool recursive = false)
    {
        if (target == null)
        {
            return;
        }

        int layer = ResolveMapLayer();
        if (layer < 0)
        {
            return;
        }

        if (recursive)
        {
            SetLayerRecursively(target.transform, layer);
            return;
        }

        target.layer = layer;
    }

    private int ResolveMapLayer()
    {
        if (cachedMapLayer != int.MinValue)
        {
            return cachedMapLayer;
        }

        cachedMapLayer = LayerMask.NameToLayer(mapLayerName);
        if (cachedMapLayer < 0 && !hasWarnedMissingMapLayer)
        {
            hasWarnedMissingMapLayer = true;
            Debug.LogWarning($"NodeMap: Layer '{mapLayerName}' was not found. Map visuals will keep their current layers.");
        }

        return cachedMapLayer;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    private void RefreshPlayerMarker()
    {
        if (!TryGetCurrentNode(out MapNodeData currentNode))
        {
            if (playerMarkerImage != null)
            {
                playerMarkerImage.enabled = false;
            }

            return;
        }

        EnsurePlayerMarker();
        if (playerMarkerImage == null)
        {
            return;
        }

        playerMarkerImage.enabled = true;
        playerMarkerImage.sprite = ResolvePlayerMarkerSprite();
        playerMarkerImage.color = playerMarkerColor;

        RectTransform markerRect = playerMarkerObject.GetComponent<RectTransform>();
        ConfigureCenteredRect(markerRect);
        float markerSize = ResolveUiElementSize(playerMarkerScale);
        markerRect.sizeDelta = new Vector2(markerSize, markerSize);

        Vector2 markerOffset = new Vector2(playerMarkerOffset.x, playerMarkerOffset.y) * GetMapScale();
        markerRect.anchoredPosition = MapToUiPosition(currentNode.position) + markerOffset + new Vector2(0f, GetPlayerMarkerBobOffsetY());
        markerRect.localRotation = Quaternion.identity;
        markerRect.localScale = Vector3.one;

        playerMarkerObject.transform.SetAsLastSibling();
    }

    private float GetPlayerMarkerBobOffsetY()
    {
        if (playerMarkerBobAmplitude <= 0f || playerMarkerBobFrequency <= 0f)
        {
            return 0f;
        }

        float bobRadians = Time.unscaledTime * playerMarkerBobFrequency * Mathf.PI * 2f;
        return Mathf.Sin(bobRadians) * playerMarkerBobAmplitude * GetMapScale();
    }

    private void CenterOnCurrentNode()
    {
        if (mapCanvas == null || mapRoot == null || mapData == null)
        {
            return;
        }

        float canvasWidth = GetCanvasWidth();
        MapNodeData targetNode = TryGetCurrentNode(out MapNodeData currentNode)
            ? currentNode
            : FindFirstReachableNode();

        if (targetNode == null)
        {
            return;
        }

        float targetX = -canvasWidth / 6f;
        float panX = targetX - MapToUiPosition(targetNode.position).x;
        SetMapRootX(panX);
        mapRoot.anchoredPosition = new Vector2(mapRoot.anchoredPosition.x, 0f);
    }

    private bool TryLoadMapFromRunState()
    {
        if (!Application.isPlaying || !loadFromRunState || !MapRunState.Instance.HasMap)
        {
            pendingMapEntryDefeatAnimationNodeId = -1;
            return false;
        }

        MapRunState runState = MapRunState.Instance;
        mapData = runState.CurrentMap;
        RefreshEnemyInfoForCurrentProgress();
        pendingMapEntryDefeatAnimationNodeId = ResolveCurrentMapEntryDefeatAnimationNodeId();
        if (!animateRecentlyDefeatedNodeOnMapEntry)
        {
            pendingMapEntryDefeatAnimationNodeId = -1;
        }

        BuildLookup();
        UpdateNodeStates();
        RebuildVisuals();
        ComputePanBounds();
        CenterOnCurrentNode();
        TryQueuePendingMapEntryDefeatAnimation();
        return true;
    }

    private void RefreshEnemyInfoForCurrentProgress()
    {
        if (mapData == null || mapData.nodes == null)
        {
            return;
        }

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            MapNodeData node = mapData.nodes[i];
            if (!IsEnemyNode(node))
            {
                continue;
            }

            if (node.enemyInfo == null)
            {
                node.enemyInfo = new EnemyInformation(node.type, GetProjectedEnemiesDefeatedForLayer(node.layer));
                continue;
            }

            node.enemyInfo.EnemyHealth = EnemyInformation.CalculateEnemyHealth(node.type, GetProjectedEnemiesDefeatedForLayer(node.layer));
        }
    }

    private int GetProjectedEnemiesDefeatedForLayer(int layerIndex)
    {
        int enemiesDefeated = PlayerStatManager.Instance != null ? PlayerStatManager.Instance.enemiesDefeated : 0;
        int anchorLayer = GetEnemyProgressAnchorLayer();
        return Mathf.Max(0, enemiesDefeated + (layerIndex - anchorLayer));
    }

    private int GetEnemyProgressAnchorLayer()
    {
        if (mapData == null || mapData.currentNodeId < 0)
        {
            return 1;
        }

        MapNodeData currentNode = mapData.FindNodeById(mapData.currentNodeId);
        if (currentNode == null)
        {
            return 1;
        }

        if (currentNode.type == MapNodeType.Start || currentNode.state == NodeState.Cleared)
        {
            return currentNode.layer + 1;
        }

        return currentNode.layer;
    }

    private int ResolveCurrentMapEntryDefeatAnimationNodeId()
    {
        if (mapData == null || mapData.currentNodeId < 0)
        {
            return -1;
        }

        MapNodeData currentNode = mapData.FindNodeById(mapData.currentNodeId);
        if (!IsEnemyNode(currentNode) || currentNode.state != NodeState.Cleared)
        {
            return -1;
        }

        return currentNode.id;
    }

    private void TryQueuePendingMapEntryDefeatAnimation()
    {
        StopPendingMapEntryDefeatAnimation();

        if (!Application.isPlaying || pendingMapEntryDefeatAnimationNodeId < 0)
        {
            return;
        }

        if (!nodesById.TryGetValue(pendingMapEntryDefeatAnimationNodeId, out MapNodeData node) ||
            (!IsEnemyNode(node) && !IsLoopStartNode(node)) ||
            node.state != NodeState.Cleared ||
            !viewsById.ContainsKey(pendingMapEntryDefeatAnimationNodeId))
        {
            pendingMapEntryDefeatAnimationNodeId = -1;
            return;
        }

        pendingMapEntryDefeatAnimationCoroutine = StartCoroutine(PlayPendingMapEntryDefeatAnimationAfterDelay());
    }

    private IEnumerator PlayPendingMapEntryDefeatAnimationAfterDelay()
    {
        float delay = ResolveMapEntryDefeatAnimationDelay();
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        int nodeId = pendingMapEntryDefeatAnimationNodeId;
        pendingMapEntryDefeatAnimationNodeId = -1;
        pendingMapEntryDefeatAnimationCoroutine = null;

        if (!nodesById.TryGetValue(nodeId, out MapNodeData node) ||
            (!IsEnemyNode(node) && !IsLoopStartNode(node)) ||
            node.state != NodeState.Cleared ||
            !viewsById.TryGetValue(nodeId, out MapNodeView view) ||
            view == null)
        {
            yield break;
        }

        if (!view.PlayEnemyDefeatVisual(recentlyDefeatedNodeUsesShadedVariant) && !hasWarnedMissingNodeDefeatAnimator)
        {
            hasWarnedMissingNodeDefeatAnimator = true;
            Debug.LogWarning("NodeMap: Pending map-entry defeat animation could not play because the node view has no Animator.");
        }
    }

    private float ResolveMapEntryDefeatAnimationDelay()
    {
        float delay = Mathf.Max(0f, mapEntryDefeatAnimationDelay);
        if (!useSceneChangerTransitionTimeForMapEntryDelay)
        {
            return delay;
        }

        SceneChanger resolvedSceneChanger = ResolveSceneChanger();
        if (resolvedSceneChanger == null)
        {
            return delay;
        }

        return Mathf.Max(delay, resolvedSceneChanger.TransitionDuration);
    }

    private void StopPendingMapEntryDefeatAnimation()
    {
        if (pendingMapEntryDefeatAnimationCoroutine != null)
        {
            StopCoroutine(pendingMapEntryDefeatAnimationCoroutine);
            pendingMapEntryDefeatAnimationCoroutine = null;
        }
    }

    private bool TryLoadEncounterScene()
    {
        
        if (config == null || !config.autoLoadEncounterScene || string.IsNullOrWhiteSpace(config.encounterSceneName))
        {
            return false;
        }

        DisableNodeHoverTooltipsForTransition();

        SceneChanger resolvedSceneChanger = ResolveSceneChanger();
        if (resolvedSceneChanger != null)
        {
            resolvedSceneChanger.ChangeScene(config.encounterSceneName);
        }
        else
        {
            SceneManager.LoadScene(config.encounterSceneName);
        }

        return true;
    }

    private void DisableNodeHoverTooltipsForTransition()
    {
        foreach (KeyValuePair<int, MapNodeView> pair in viewsById)
        {
            if (pair.Value == null)
            {
                continue;
            }

            MapNodeHoverTooltip hoverTooltip = pair.Value.GetComponent<MapNodeHoverTooltip>();
            if (hoverTooltip != null)
            {
                hoverTooltip.SetHoverEnabled(false);
            }
        }
    }

    private bool TryGetCurrentNode(out MapNodeData currentNode)
    {
        currentNode = null;
        return mapData != null && nodesById.TryGetValue(mapData.currentNodeId, out currentNode);
    }

    private MapNodeData FindFirstReachableNode()
    {
        MapNodeData target = null;
        foreach (KeyValuePair<int, MapNodeData> entry in nodesById)
        {
            if (entry.Value.state != NodeState.Reachable)
            {
                continue;
            }

            if (target == null || entry.Value.layer < target.layer)
            {
                target = entry.Value;
            }
        }

        return target;
    }

    private void SaveRuntimeState()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        MapRunState.Instance.SaveMap(mapData);
    }

    private SceneChanger ResolveSceneChanger()
    {
        if (sceneChanger != null)
        {
            return sceneChanger;
        }

        sceneChanger = FindFirstObjectByType<SceneChanger>(FindObjectsInactive.Include);
        return sceneChanger;
    }

    private void EnsureEventSystem()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        }

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
        {
            legacyModule.enabled = false;
        }
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
}
