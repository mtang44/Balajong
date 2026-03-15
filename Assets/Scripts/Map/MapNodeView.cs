using UnityEngine;
using UnityEngine.UI;

public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Image nodeImage;
    [SerializeField] private Button nodeButton;
    [SerializeField] private MapNodeHoverTooltip hoverTooltip;
    [SerializeField, Range(0f, 1f)] private float lockedNodeBrightness = 0.4f;

    [Header("Defeat Animation")]
    [SerializeField] private Animator nodeAnimator;
    [SerializeField] private string enemyDeadBoolParameterName = "EnemyDead";
    [SerializeField] private string enemyShadedBoolParameterName = "Shaded";
    [SerializeField] private string mapDeadBoolParameterName = "MapDead";

    private NodeMap owner;
    private int nodeId;

    public int NodeId => nodeId;

    // Initializes the node view with the given data and settings
    public void Setup(NodeMap mapOwner, MapNodeData nodeData, Sprite nodeSprite)
    {
        owner = mapOwner;
        nodeId = nodeData.id;

        CacheComponents();

        if (nodeImage != null)
        {
            if (nodeSprite != null)
            {
                nodeImage.sprite = nodeSprite;
            }

            nodeImage.color = Color.white;
            nodeImage.preserveAspect = true;
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

        EnsureHoverTooltip();
        hoverTooltip.Setup(nodeImage, nodeButton, nodeData.type);

        gameObject.name = $"MapNode_{nodeData.id}_{nodeData.type}";
        SetState(nodeData.state);
    }

    public void SetEnemyHealthText(string newText)
    {
        EnsureHoverTooltip();
        if (hoverTooltip != null)
        {
            hoverTooltip.SetEnemyHealthText(newText);
        }
    }

    public void SetEnemyNameText(string newText)
    {
        EnsureHoverTooltip();
        if (hoverTooltip != null)
        {
            hoverTooltip.SetEnemyNameText(newText);
        }
    }

    public void SetBattlePayoutText(string newText)
    {
        EnsureHoverTooltip();
        if (hoverTooltip != null)
        {
            hoverTooltip.SetBattlePayoutText(newText);
        }
    }

    public bool SetEnemyAliveVisual()
    {
        return ApplyDefeatAnimationState(isMapDead: false, isEnemyDead: false, isShaded: false);
    }

    public bool SetEnemyAlreadyDefeatedVisual()
    {
        return ApplyDefeatAnimationState(isMapDead: true, isEnemyDead: false, isShaded: false);
    }

    public bool PlayEnemyDefeatVisual(bool shadedValue)
    {
        return ApplyDefeatAnimationState(isMapDead: false, isEnemyDead: true, isShaded: shadedValue);
    }

    // Sets the interaction state of the node based on its NodeState
    public void SetState(NodeState state)
    {
        bool isUnlocked = state != NodeState.Locked;
        bool isCleared = state == NodeState.Cleared;
        bool isCurrentNode = owner != null && owner.IsCurrentNode(nodeId);
        bool allowHover = isUnlocked && !isCleared && !isCurrentNode;

        if (nodeButton != null)
        {
            nodeButton.interactable = isUnlocked;
        }

        Collider2D nodeCollider = GetComponent<Collider2D>();
        if (nodeCollider != null)
        {
            nodeCollider.enabled = isUnlocked;
        }

        if (nodeImage != null)
        {
            float brightness = state == NodeState.Locked ? Mathf.Clamp01(lockedNodeBrightness) : 1f;
            nodeImage.color = new Color(brightness, brightness, brightness, 1f);
        }

        if (hoverTooltip != null)
        {
            hoverTooltip.SetHoverEnabled(allowHover);
        }
    }

    private void OnClicked()
    {
        if (owner != null)
        {
            owner.OnNodeClicked(nodeId);
        }

        if (hoverTooltip != null)
        {
            hoverTooltip.HideTooltip();
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

    private void CacheComponents()
    {
        nodeImage ??= GetComponentInChildren<Image>();
        nodeButton ??= GetComponent<Button>();
        nodeAnimator ??= GetComponent<Animator>();
    }

    private void EnsureHoverTooltip()
    {
        if (hoverTooltip == null)
        {
            hoverTooltip = GetComponent<MapNodeHoverTooltip>();
        }

        if (hoverTooltip == null)
        {
            hoverTooltip = gameObject.AddComponent<MapNodeHoverTooltip>();
        }
    }

    private bool ApplyDefeatAnimationState(bool isMapDead, bool isEnemyDead, bool isShaded)
    {
        Animator resolvedAnimator = ResolveNodeAnimator();
        if (resolvedAnimator == null)
        {
            return false;
        }

        // Set shaded first so the EnemyDead transition can branch into the right variant in the same frame.
        SetAnimatorBoolIfPresent(resolvedAnimator, mapDeadBoolParameterName, isMapDead);
        SetAnimatorBoolIfPresent(resolvedAnimator, enemyShadedBoolParameterName, isShaded);
        SetAnimatorBoolIfPresent(resolvedAnimator, enemyDeadBoolParameterName, isEnemyDead);
        return true;
    }

    private Animator ResolveNodeAnimator()
    {
        if (nodeAnimator == null)
        {
            nodeAnimator = GetComponent<Animator>();
        }

        return nodeAnimator;
    }

    private static void SetAnimatorBoolIfPresent(Animator animator, string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        for (int i = 0; i < animator.parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = animator.parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

}
