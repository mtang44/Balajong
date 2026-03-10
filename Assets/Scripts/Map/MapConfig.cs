using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct NodeTypeWeight
{
    public MapNodeType type;
    [Min(0)] public int weight;
}

[CreateAssetMenu(fileName = "MapConfig", menuName = "Balajong/Map Config")]
public class MapConfig : ScriptableObject
{
    // Map generation settings
    [Header("Generation")]
    [Min(3)] public int layerCount = 8;
    [Min(1)] public int minNodesPerMiddleLayer = 2;
    [Min(1)] public int maxNodesPerMiddleLayer = 4;
    [Min(1)] public int guaranteedPathCount = 3;
    [Range(0f, 1f)] public float extraConnectionChance = 0.25f;
    [Min(1)] public int maxOutgoingConnections = 3;

    [Header("Layout")]
    [Min(0.1f)] public float layerSpacing = 3f;
    [Min(0.1f)] public float nodeSpacing = 2f;
    [Min(0f)] public float nodeJitter = 0.2f;

    [Header("Encounter Type Weights")]
    public List<NodeTypeWeight> nodeTypeWeights = new List<NodeTypeWeight>
    {
        new NodeTypeWeight { type = MapNodeType.Battle, weight = 70 },
        new NodeTypeWeight { type = MapNodeType.Elite, weight = 17 },
        new NodeTypeWeight { type = MapNodeType.Rest, weight = 13 },
    };

    [Header("Flow")]
    public bool autoLoadEncounterScene = false;
    public bool completeNodeImmediatelyInMapScene = true;
    public string encounterSceneName = string.Empty;

    [Header("Visuals")]
    [Min(0.05f)] public float nodeScale = 0.8f;
    [Min(0.01f)] public float lineWidth = 0.08f;
    public Color inactiveConnectionColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
    public Color activeConnectionColor = new Color(0.9f, 0.9f, 0.9f, 1f);
}
