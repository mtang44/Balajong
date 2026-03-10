using UnityEngine;

public class MapNodeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer nodeSpriteRenderer;

    private NodeMap owner;
    private int nodeId;
    private MapNodeType nodeType;

    public int NodeId => nodeId;

    // Initializes the node view with the given data and settings
    public void Setup(NodeMap mapOwner, MapNodeData nodeData, Sprite defaultSprite, int sortingOrder)
    {
        owner = mapOwner;
        nodeId = nodeData.id;
        nodeType = nodeData.type;

        nodeSpriteRenderer ??= GetComponentInChildren<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
        
        if (nodeSpriteRenderer.sprite == null)
        {
            nodeSpriteRenderer.sprite = defaultSprite;
        }

        nodeSpriteRenderer.sortingOrder = sortingOrder;
        
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>();
        }

        gameObject.name = $"MapNode_{nodeData.id}_{nodeData.type}";
        SetState(nodeData.state);
    }

    // Sets the visual state of the node based on its NodeState
    public void SetState(NodeState state)
    {
        Collider2D nodeCollider = GetComponent<Collider2D>();
        if (nodeCollider != null)
        {
            nodeCollider.enabled = state != NodeState.Locked;
        }

        if (nodeSpriteRenderer == null) return;

        Color baseColor = GetTypeColor(nodeType);
        nodeSpriteRenderer.color = state switch
        {
            NodeState.Cleared => Color.Lerp(baseColor, Color.white, 0.5f),
            NodeState.Reachable => baseColor,
            _ => Color.Lerp(baseColor, Color.black, 0.65f)
        };
    }

    // Called when the node is clicked by the player
    private void OnMouseUpAsButton()
    {
        if (owner != null)
        {
            owner.OnNodeClicked(nodeId);
        }
    }

    // Returns the color associated with the given node type
    private static Color GetTypeColor(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Start:
                return new Color(0.35f, 0.8f, 0.95f, 1f);
            case MapNodeType.Battle:
                return new Color(0.9f, 0.35f, 0.35f, 1f);
            case MapNodeType.Elite:
                return new Color(1f, 0.58f, 0.15f, 1f);
            case MapNodeType.Rest:
                return new Color(0.25f, 0.8f, 0.4f, 1f);
            case MapNodeType.Boss:
                return new Color(0.65f, 0.2f, 0.2f, 1f);
            default:
                return Color.white;
        }
    }
}
