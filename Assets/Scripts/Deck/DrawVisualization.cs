using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrawVisualization : MonoBehaviour
{
    public static DrawVisualization Instance;

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

    /// <summary>
    /// Animate a list of tiles from the draw origin to their already-assigned local positions.
    /// Call this after sortHand() has set the final positions on the tiles.
    /// </summary>
    public void AnimateDeal(List<GameObject> tiles)
    {
        if (drawOrigin == null || tiles == null || tiles.Count == 0) return;
        StartCoroutine(DealSequence(new List<GameObject>(tiles)));
    }

    private IEnumerator DealSequence(List<GameObject> tiles)
    {
        // Sort tiles left-to-right by their final hand X position (already set by sortHand).
        tiles.Sort((a, b) =>
        {
            if (a == null || b == null) return 0;
            return a.transform.localPosition.x.CompareTo(b.transform.localPosition.x);
        });

        // Capture every tile's final destination before moving anything.
        var destinations = new (Vector3 pos, Quaternion rot)[tiles.Count];
        Vector3 originLocalPos = Vector3.zero;
        bool originComputed = false;

        for (int i = 0; i < tiles.Count; i++)
        {
            GameObject tile = tiles[i];
            if (tile == null) continue;

            destinations[i] = (tile.transform.localPosition, tile.transform.localRotation);

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

            StartCoroutine(MoveTile(tile, originLocalPos, destinations[i].pos, destinations[i].rot));
            yield return new WaitForSeconds(dealDelay);
        }
    }

    private IEnumerator MoveTile(GameObject tile, Vector3 startLocalPos, Vector3 endLocalPos, Quaternion endLocalRot)
    {
        if (tile == null) yield break;

        // Tile starts turned 90 degrees on Y so it appears to spin into place.
        Quaternion startRot = endLocalRot * Quaternion.Euler(0f, -90f, 0f);

        float elapsed = 0f;
        while (elapsed < dealDuration)
        {
            if (tile == null) yield break;
            elapsed += Time.deltaTime;
            float t = dealCurve.Evaluate(Mathf.Clamp01(elapsed / dealDuration));

            // Arc the tile upward along a sine curve for a lively feel.
            Vector3 pos = Vector3.Lerp(startLocalPos, endLocalPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            tile.transform.localPosition = pos;

            tile.transform.localRotation = Quaternion.Slerp(startRot, endLocalRot, t);

            yield return null;
        }

        if (tile != null)
        {
            SoundManager.Instance.playDrawSound();
            tile.transform.localPosition = endLocalPos;
            tile.transform.localRotation = endLocalRot;
        }
    }
}
