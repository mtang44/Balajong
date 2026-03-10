using UnityEngine;

public class MapNodeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer nodeSpriteRenderer;

    private NodeMap owner;
    private int nodeId;
    private MapNodeType nodeType;

    public int NodeId => nodeId;

    public void Setup(NodeMap mapOwner, MapNodeData nodeData, Sprite defaultSprite, int sortingOrder)
    {
        owner = mapOwner;
        nodeId = nodeData.id;
        nodeType = nodeData.type;

        if (nodeSpriteRenderer == null)
        {
            nodeSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (nodeSpriteRenderer == null)
        {
            nodeSpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (nodeSpriteRenderer.sprite == null && defaultSprite != null)
        {
            nodeSpriteRenderer.sprite = defaultSprite;
        }

        nodeSpriteRenderer.sortingOrder = sortingOrder;
        EnsureClickableCollider();

        gameObject.name = $"MapNode_{nodeData.id}_{nodeData.type}";
        SetState(nodeData.state);
    }

    public void SetState(NodeState state)
    {
        Collider2D nodeCollider2D = GetComponent<Collider2D>();
        if (nodeCollider2D != null)
        {
            nodeCollider2D.enabled = state != NodeState.Locked;
        }

        Collider nodeCollider3D = GetComponent<Collider>();
        if (nodeCollider3D != null)
        {
            nodeCollider3D.enabled = state != NodeState.Locked;
        }

        if (nodeSpriteRenderer == null)
        {
            return;
        }

        Color baseColor = GetTypeColor(nodeType);
        Color finalColor;

        switch (state)
        {
            case NodeState.Cleared:
                finalColor = Color.Lerp(baseColor, Color.white, 0.45f);
                break;
            case NodeState.Reachable:
                finalColor = baseColor;
                break;
            default:
                finalColor = Color.Lerp(baseColor, Color.black, 0.7f);
                break;
        }

        nodeSpriteRenderer.color = finalColor;
    }

    private void EnsureClickableCollider()
    {
        if (GetComponent<Collider2D>() == null && GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>();
        }
    }

    private void OnMouseUpAsButton()
    {
        if (owner != null)
        {
            owner.OnNodeClicked(nodeId);
        }
    }

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
