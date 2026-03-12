using System.Collections.Generic;
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
    [SerializeField] private float mapUnitsToPixels = 120f;
    [SerializeField] private float nodeUiSize = 96f;
    [SerializeField] private float lineUiWidth = 10f;
    [SerializeField] private Sprite playerMarkerSprite;
    [SerializeField] private Color playerMarkerColor = new Color(0.2f, 0.95f, 1f, 1f);
    [SerializeField] private Vector3 playerMarkerOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private float playerMarkerScale = 0.55f;
    [SerializeField] private Vector2 canvasReferenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private bool autoCreateCanvasWhenMissing = true;

    [Header("Runtime")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool loadFromRunState = true;
    [SerializeField] private int fixedSeed = -1;

    private NodeMapData mapData;
    private System.Random random;
    private Sprite generatedCircleSprite;
    private GameObject playerMarkerObject;
    private Image playerMarkerImage;
    private bool hasWarnedNonUiNodePrefab;
    private int cachedMapLayer = int.MinValue;
    private bool hasWarnedMissingMapLayer;

    private readonly Dictionary<int, MapNodeData> nodesById = new Dictionary<int, MapNodeData>();
    private readonly Dictionary<int, MapNodeView> viewsById = new Dictionary<int, MapNodeView>();
    private readonly List<ConnectionVisual> connectionVisuals = new List<ConnectionVisual>();

    // Represents a visual connection between two nodes
    private struct ConnectionVisual
    {
        public int fromId;
        public int toId;
        public RectTransform rect;
        public Image image;
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
        {
            return;
        }

        if (Application.isPlaying && loadFromRunState && MapRunState.Instance.HasMap)
        {
            mapData = MapRunState.Instance.CurrentMap;
            BuildLookup();
            UpdateNodeStates();
            RebuildVisuals();
            return;
        }

        GenerateNewMap();
    }

    private void OnValidate()
    {
        cachedMapLayer = int.MinValue;
        hasWarnedMissingMapLayer = false;
        hasWarnedNonUiNodePrefab = false;
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
        if (!nodesById.TryGetValue(nodeId, out MapNodeData node))
        {
            return;
        }

        // Prevent clicking on nodes in the current layer or any previous layers
        if (nodesById.TryGetValue(mapData.currentNodeId, out MapNodeData currentNode))
        {
            if (node.layer <= currentNode.layer)
            {
                return;
            }
        }

        if (node.state != NodeState.Reachable)
        {
            return;
        }

        mapData.currentNodeId = node.id;
        SaveRuntimeState();

        if (config.autoLoadEncounterScene && !string.IsNullOrWhiteSpace(config.encounterSceneName))
        {
            SaveRuntimeState();

            SceneChanger resolvedSceneChanger = ResolveSceneChanger();
            if (resolvedSceneChanger != null)
            {
                resolvedSceneChanger.ChangeScene(config.encounterSceneName);
            }
            else
            {
                SceneManager.LoadScene(config.encounterSceneName);
            }
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
        if (!nodesById.TryGetValue(mapData.currentNodeId, out MapNodeData currentNode))
        {
            return;
        }

        // Lock all nodes in the current layer and previous layers (except the current node itself)
        // This ensures nodes in the same row as the player are dimmed/unreachable
        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            MapNodeData node = mapData.nodes[i];
            
            // Skip the current node
            if (node.id == currentNode.id) continue;

            // For nodes in current or previous layers
            if (node.layer <= currentNode.layer)
            {
                // Lock them unless they've been completed (Cleared)
                if (node.state != NodeState.Cleared)
                {
                    node.state = NodeState.Locked;
                }
            }
        }
    }

    public NodeMapData GetMapData() => mapData;

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
        for (int pass = 0; pass < 8; pass++)
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
        List<(MapNodeData node, float barycenter)> nodePositions = new List<(MapNodeData, float)>();

        foreach (MapNodeData node in currentLayer)
        {
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

            float barycenter = count > 0 ? sum / count : (byPredecessors ? currentLayer.IndexOf(node) : node.position.y);
            nodePositions.Add((node, barycenter));
        }

        nodePositions.Sort((a, b) => a.barycenter.CompareTo(b.barycenter));
        for (int i = 0; i < nodePositions.Count; i++)
            currentLayer[i] = nodePositions[i].node;

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
        Sprite nodeSprite = ResolveNodeSprite();

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
            float size = Mathf.Max(4f, nodeUiSize * Mathf.Max(0.05f, config.nodeScale));
            nodeRect.sizeDelta = new Vector2(size, size);
            nodeRect.anchoredPosition = MapToUiPosition(node.position);
            nodeRect.localRotation = Quaternion.identity;
            nodeRect.localScale = Vector3.one;
        }

        view.Setup(this, node, nodeSprite, 0);

        return view;
    }

    private void CreateConnectionVisual(MapNodeData from, MapNodeData to)
    {
        GameObject lineObject = new GameObject($"Edge_{from.id}_{to.id}", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(mapRoot, false);
        lineObject.transform.SetAsFirstSibling();
        ApplyMapLayer(lineObject);

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        ConfigureCenteredRect(lineRect);
        PositionConnectionRect(lineRect, MapToUiPosition(from.position), MapToUiPosition(to.position));

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.raycastTarget = false;
        lineImage.color = config.inactiveConnectionColor;

        connectionVisuals.Add(new ConnectionVisual
        {
            fromId = from.id,
            toId = to.id,
            rect = lineRect,
            image = lineImage
        });
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
                visual.image == null)
            {
                continue;
            }

            bool isActive = fromNode.state == NodeState.Cleared &&
                            (toNode.state == NodeState.Reachable || toNode.state == NodeState.Cleared);
            Color color = isActive ? config.activeConnectionColor : config.inactiveConnectionColor;
            visual.image.color = color;

            if (visual.rect != null)
            {
                PositionConnectionRect(visual.rect, MapToUiPosition(fromNode.position), MapToUiPosition(toNode.position));
            }
        }

        RefreshPlayerMarker();
    }

    private Sprite ResolveNodeSprite() => defaultNodeSprite != null ? defaultNodeSprite : GetOrCreateCircleSprite();

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
        if (mapRoot == null || (playerMarkerObject != null && playerMarkerImage != null)) return;

        playerMarkerObject = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        playerMarkerObject.transform.SetParent(mapRoot, false);
        ApplyMapLayer(playerMarkerObject);

        playerMarkerImage = playerMarkerObject.GetComponent<Image>();
        playerMarkerImage.raycastTarget = false;

        RectTransform markerRect = playerMarkerObject.GetComponent<RectTransform>();
        ConfigureCenteredRect(markerRect);
    }

    private Vector2 MapToUiPosition(Vector2 mapPosition)
    {
        float scale = Mathf.Max(1f, mapUnitsToPixels);
        return mapPosition * scale;
    }

    private void PositionConnectionRect(RectTransform rect, Vector2 from, Vector2 to)
    {
        if (rect == null)
        {
            return;
        }

        Vector2 delta = to - from;
        float length = Mathf.Max(1f, delta.magnitude);
        float width = Mathf.Max(1f, lineUiWidth);

        rect.sizeDelta = new Vector2(length, width);
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
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
        if (mapData == null || !nodesById.TryGetValue(mapData.currentNodeId, out MapNodeData currentNode))
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
        float markerSize = Mathf.Max(4f, nodeUiSize * Mathf.Max(0.05f, playerMarkerScale));
        markerRect.sizeDelta = new Vector2(markerSize, markerSize);

        Vector2 markerOffset = new Vector2(playerMarkerOffset.x, playerMarkerOffset.y) * Mathf.Max(1f, mapUnitsToPixels);
        markerRect.anchoredPosition = MapToUiPosition(currentNode.position) + markerOffset;
        markerRect.localRotation = Quaternion.identity;
        markerRect.localScale = Vector3.one;

        playerMarkerObject.transform.SetAsLastSibling();
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

        sceneChanger = FindObjectOfType<SceneChanger>(true);
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
            eventSystem = FindObjectOfType<EventSystem>(true);
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
