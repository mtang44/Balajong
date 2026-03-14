using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CheckRackHandHoverPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TileSourceMode
    {
        FullHandAndBonuses,
        SelectedOnly
    }

    [Header("References")]
    [SerializeField] private Button targetButton;
    [SerializeField] private TileSourceMode tileSourceMode = TileSourceMode.FullHandAndBonuses;

    [Header("Glint")]
    [SerializeField] private Color glintColor = new Color(1f, 0.96f, 0.82f, 1f);
    [SerializeField, Range(0f, 1f)] private float tintStrength = 0.75f;
    [SerializeField, Range(0f, 1f)] private float brightnessBoost = 0.14f;
    [SerializeField, Min(0.01f)] private float tileGlintDuration = 0.28f;
    [SerializeField, Min(0f)] private float tileGlintStagger = 0.05f;
    [SerializeField, Min(0f)] private float loopPause = 0.4f;

    private readonly List<GameObject> activeTiles = new List<GameObject>();
    private readonly List<GameObject> refreshedTiles = new List<GameObject>();
    private MaterialPropertyBlock propertyBlock;

    private bool isHovering;
    private float hoverStartTime;

    private static readonly string[] SupportedColorProperties = { "_BaseColor", "_Color" };
    private const string EmissionColorProperty = "_EmissionColor";

    private void Reset()
    {
        targetButton = GetComponent<Button>();
    }

    private void Awake()
    {
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }

        EnsurePropertyBlock();
    }

    private void Update()
    {
        if (!isHovering)
        {
            return;
        }

        if (!CanPreview())
        {
            StopPreview();
            return;
        }

        RefreshActiveTiles();
        ApplyGlintWave();
    }

    public void Configure(Button button, TileSourceMode sourceMode = TileSourceMode.FullHandAndBonuses)
    {
        targetButton = button != null ? button : GetComponent<Button>();
        tileSourceMode = sourceMode;
    }

    public void SetGlintColor(Color color)
    {
        glintColor = color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanPreview())
        {
            return;
        }

        isHovering = true;
        hoverStartTime = Time.unscaledTime;
        RefreshActiveTiles();
        ApplyGlintWave();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopPreview();
    }

    private void OnDisable()
    {
        StopPreview();
    }

    private bool CanPreview()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Score)
        {
            return false;
        }

        return targetButton == null || (targetButton.isActiveAndEnabled && targetButton.interactable);
    }

    private void StopPreview()
    {
        if (!isHovering && activeTiles.Count == 0)
        {
            return;
        }

        isHovering = false;
        ClearHighlights(activeTiles);
        activeTiles.Clear();
        refreshedTiles.Clear();
    }

    private void RefreshActiveTiles()
    {
        RefreshTargetTiles(refreshedTiles);
        ClearHighlightsForMissingTiles(activeTiles, refreshedTiles);

        activeTiles.Clear();
        activeTiles.AddRange(refreshedTiles);
    }

    private void RefreshTargetTiles(List<GameObject> targetTiles)
    {
        targetTiles.Clear();

        DeckManager deckManager = DeckManager.Instance;
        if (deckManager == null)
        {
            return;
        }

        switch (tileSourceMode)
        {
            case TileSourceMode.SelectedOnly:
                AddTiles(targetTiles, deckManager.selectedTiles);
                break;
            case TileSourceMode.FullHandAndBonuses:
            default:
                AddTiles(targetTiles, deckManager.hand);
                AddTiles(targetTiles, deckManager.flowerTiles);
                AddTiles(targetTiles, deckManager.seasonTiles);
                break;
        }
    }

    private static void AddTiles(List<GameObject> targetTiles, List<GameObject> sourceTiles)
    {
        if (sourceTiles == null)
        {
            return;
        }

        foreach (GameObject tile in sourceTiles)
        {
            if (tile != null)
            {
                targetTiles.Add(tile);
            }
        }
    }

    private void ApplyGlintWave()
    {
        if (activeTiles.Count == 0)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, tileGlintDuration);
        float stagger = Mathf.Max(0f, tileGlintStagger);
        float cycleDuration = ((activeTiles.Count - 1) * stagger) + duration + Mathf.Max(0f, loopPause);
        float cycleTime = cycleDuration > 0f ? Mathf.Repeat(Time.unscaledTime - hoverStartTime, cycleDuration) : 0f;

        for (int i = 0; i < activeTiles.Count; i++)
        {
            float localTime = cycleTime - (i * stagger);
            float intensity = EvaluateGlintIntensity(localTime, duration);
            ApplyTileHighlight(activeTiles[i], intensity);
        }
    }

    private static float EvaluateGlintIntensity(float localTime, float duration)
    {
        if (localTime < 0f || localTime > duration)
        {
            return 0f;
        }

        float normalizedTime = localTime / duration;
        return Mathf.Sin(normalizedTime * Mathf.PI);
    }

    private void ApplyTileHighlight(GameObject tile, float intensity)
    {
        if (tile == null)
        {
            return;
        }

        Renderer[] renderers = tile.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
            {
                continue;
            }

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                {
                    continue;
                }

                ApplyRendererHighlight(renderer, materialIndex, material, intensity);
            }
        }
    }

    private void ApplyRendererHighlight(Renderer renderer, int materialIndex, Material material, float intensity)
    {
        if (renderer == null || material == null)
        {
            return;
        }

        EnsurePropertyBlock();

        string colorPropertyName = GetColorPropertyName(material);
        bool hasEmission = material.HasProperty(EmissionColorProperty);
        if (string.IsNullOrEmpty(colorPropertyName) && !hasEmission)
        {
            return;
        }

        propertyBlock.Clear();
        if (intensity > 0.001f)
        {
            if (!string.IsNullOrEmpty(colorPropertyName))
            {
                Color baseColor = material.GetColor(colorPropertyName);
                Color tintedColor = Color.Lerp(baseColor, glintColor, Mathf.Clamp01(tintStrength) * intensity);
                float brightness = 1f + (Mathf.Clamp01(brightnessBoost) * intensity);
                tintedColor = new Color(tintedColor.r * brightness, tintedColor.g * brightness, tintedColor.b * brightness, baseColor.a);
                propertyBlock.SetColor(colorPropertyName, tintedColor);
            }

            if (hasEmission)
            {
                propertyBlock.SetColor(EmissionColorProperty, glintColor * (0.15f * intensity));
            }
        }

        renderer.SetPropertyBlock(propertyBlock, materialIndex);
    }

    private void EnsurePropertyBlock()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void ClearHighlightsForMissingTiles(List<GameObject> previousTiles, List<GameObject> nextTiles)
    {
        foreach (GameObject tile in previousTiles)
        {
            if (tile != null && !nextTiles.Contains(tile))
            {
                ApplyTileHighlight(tile, 0f);
            }
        }
    }

    private void ClearHighlights(List<GameObject> tiles)
    {
        foreach (GameObject tile in tiles)
        {
            if (tile != null)
            {
                ApplyTileHighlight(tile, 0f);
            }
        }
    }

    private static string GetColorPropertyName(Material material)
    {
        foreach (string propertyName in SupportedColorProperties)
        {
            if (material.HasProperty(propertyName))
            {
                return propertyName;
            }
        }

        return null;
    }
}