using UnityEngine;
using UnityEngine.UI;

public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Image nodeImage;
    [SerializeField] private Button nodeButton;

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

        nodeImage ??= GetComponentInChildren<Image>();
        nodeButton ??= GetComponent<Button>();

        if (nodeImage != null)
        {
            if (nodeImage.sprite == null)
            {
                nodeImage.sprite = defaultSprite;
            }

            nodeImage.raycastTarget = true;

            if (nodeButton == null)
            {
                nodeButton = gameObject.AddComponent<Button>();
            }

            nodeButton.transition = Selectable.Transition.None;
            nodeButton.targetGraphic = nodeImage;
            nodeButton.onClick.RemoveListener(OnClicked);
            nodeButton.onClick.AddListener(OnClicked);
        }

        gameObject.name = $"MapNode_{nodeData.id}_{nodeData.type}";
        SetState(nodeData.state);
    }

    // Sets the visual state of the node based on its NodeState
    public void SetState(NodeState state)
    {
        if (nodeButton != null)
        {
            nodeButton.interactable = state != NodeState.Locked;
        }

        Collider2D nodeCollider = GetComponent<Collider2D>();
        if (nodeCollider != null)
        {
            nodeCollider.enabled = state != NodeState.Locked;
        }

        Color baseColor = GetTypeColor(nodeType);
        Color nodeColor = state switch
        {
            NodeState.Cleared => Color.Lerp(baseColor, Color.white, 0.5f),
            NodeState.Reachable => baseColor,
            _ => Color.Lerp(baseColor, Color.black, 0.65f)
        };

        if (nodeImage != null)
        {
            nodeImage.color = nodeColor;
        }
    }

    private void OnClicked()
    {
        if (owner != null)
        {
            owner.OnNodeClicked(nodeId);
        }
    }

    // Called when the node is clicked by the player
    private void OnMouseUpAsButton()
    {
        if (nodeButton != null)
        {
            return;
        }

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
