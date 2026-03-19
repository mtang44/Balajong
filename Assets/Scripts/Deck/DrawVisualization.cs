using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrawVisualization : MonoBehaviour
{
    public static DrawVisualization Instance;

    private readonly HashSet<GameObject> dealLockedTiles = new HashSet<GameObject>();

    [Tooltip("Tiles spawn here and travel to their hand positions.")]
    public Transform drawOrigin;

    [Tooltip("Seconds between each tile being dealt.")]
    [SerializeField] private float dealDelay = 0.07f;

    [Tooltip("Seconds each tile takes to travel to its position.")]
    [SerializeField] private float dealDuration = 0.28f;

    [Tooltip("Height of the arc the tile follows during travel.")]
    [SerializeField] private float arcHeight = 0.35f;

    [SerializeField] private AnimationCurve dealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDisable()
    {
        dealLockedTiles.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        dealLockedTiles.Clear();
    }

    /// <summary>
    /// Animate a list of tiles from the draw origin to their already-assigned local positions.
    /// Call this after sortHand() has set the final positions on the tiles.
    /// </summary>
    public void AnimateDeal(List<GameObject> tiles)
    {
        if (drawOrigin == null || tiles == null || tiles.Count == 0) return;
        StartCoroutine(DealSequence(new List<GameObject>(tiles)));
    }

    public bool IsTileDealLocked(GameObject tile)
    {
        return tile != null && dealLockedTiles.Contains(tile);
    }

    private IEnumerator DealSequence(List<GameObject> tiles)
    {
        // Sort tiles left-to-right by their current intended target slot.
        tiles.Sort((a, b) =>
        {
            if (a == null || b == null) return 0;

            TryResolveCurrentTargetPose(a, out Vector3 aPos, out _);
            TryResolveCurrentTargetPose(b, out Vector3 bPos, out _);
            return aPos.x.CompareTo(bPos.x);
        });

        Vector3 originLocalPos = Vector3.zero;
        bool originComputed = false;

        for (int i = 0; i < tiles.Count; i++)
        {
            GameObject tile = tiles[i];
            if (tile == null) continue;
            dealLockedTiles.Add(tile);

            if (!originComputed)
            {
                Transform parent = tile.transform.parent;
                originLocalPos = parent != null
                    ? parent.InverseTransformPoint(drawOrigin.position)
                    : drawOrigin.position;
                originComputed = true;
            }
        }

        // Teleport ALL tiles to the draw origin immediately so none flash at hand positions.
        foreach (GameObject tile in tiles)
        {
            if (tile != null)
                tile.transform.localPosition = originLocalPos;
        }

        // Deal each tile to its destination, staggered left to right.
        for (int i = 0; i < tiles.Count; i++)
        {
            GameObject tile = tiles[i];
            if (tile == null) continue;

            StartCoroutine(MoveTile(tile, originLocalPos));
            yield return new WaitForSeconds(dealDelay);
        }
    }

    private IEnumerator MoveTile(GameObject tile, Vector3 startLocalPos)
    {
        if (tile == null)
        {
            yield break;
        }

        TryResolveCurrentTargetPose(tile, out Vector3 initialEndLocalPos, out Quaternion initialEndLocalRot);

        // Tile starts turned 90 degrees on Y so it appears to spin into place.
        Quaternion startRot = initialEndLocalRot * Quaternion.Euler(0f, -90f, 0f);

        float elapsed = 0f;
        while (elapsed < dealDuration)
        {
            if (tile == null)
            {
                dealLockedTiles.Remove(tile);
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = dealCurve.Evaluate(Mathf.Clamp01(elapsed / dealDuration));

            TryResolveCurrentTargetPose(tile, out Vector3 endLocalPos, out Quaternion endLocalRot);

            // Arc the tile upward along a sine curve for a lively feel.
            Vector3 pos = Vector3.Lerp(startLocalPos, endLocalPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            tile.transform.localPosition = pos;

            tile.transform.localRotation = Quaternion.Slerp(startRot, endLocalRot, t);

            yield return null;
        }

        if (tile != null)
        {
            TryResolveCurrentTargetPose(tile, out Vector3 finalLocalPos, out Quaternion finalLocalRot);
            SoundManager.Instance.playDrawSound();
            tile.transform.localPosition = finalLocalPos;
            tile.transform.localRotation = finalLocalRot;
        }

        dealLockedTiles.Remove(tile);
    }

    private static void TryResolveCurrentTargetPose(GameObject tile, out Vector3 localPos, out Quaternion localRot)
    {
        if (tile == null)
        {
            localPos = Vector3.zero;
            localRot = Quaternion.identity;
            return;
        }

        DeckManager deckManager = DeckManager.Instance;
        if (deckManager != null && deckManager.TryGetIntendedTilePose(tile, out localPos, out localRot))
        {
            return;
        }

        localPos = tile.transform.localPosition;
        localRot = tile.transform.localRotation;
    }
}
