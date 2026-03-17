using System;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Tooltip("The tile prefab to instantiate. Must have MahjongTileHolder and TileGenerator components.")]
    [SerializeField] private GameObject tilePrefab;

    [Tooltip("World-space transforms that define where each tile slot appears.")]
    [SerializeField] private Transform[] slotTransforms;

    [Tooltip("Optional parent transform for spawned tiles. Leave empty to use this GameObject.")]
    [SerializeField] private Transform spawnParent;

    [Tooltip("Optional parent override for slot 0 (first tile), for example Tile1.")]
    [SerializeField] private Transform tile1Parent;

    [Tooltip("Optional parent override for slot 1 (second tile), for example Tile2.")]
    [SerializeField] private Transform tile2Parent;

    [Tooltip("ShopTilePurchaseUI overlays that should be re-enabled when tiles are rerolled.")]
    [SerializeField] private ShopTilePurchaseUI[] slotOverlays;

    [Tooltip("Local scale applied to each spawned tile prefab.")]
    [SerializeField] private Vector3 tileScale = Vector3.one;

    [Tooltip("Euler rotation offset applied to each spawned tile prefab.")]
    [SerializeField] private Vector3 tileRotationEuler = Vector3.zero;

    private GameObject[] spawnedTiles;

    public event Action TilesChanged;

    private void Start()
    {
        RerollTiles();
    }

    public void RerollTiles()
    {
        if (slotOverlays != null)
        {
            for (int i = 0; i < slotOverlays.Length; i++)
            {
                if (slotOverlays[i] != null)
                {
                    slotOverlays[i].gameObject.SetActive(true);
                }
            }
        }

        if (tilePrefab == null)
        {
            Debug.LogWarning("TileSpawner: no tilePrefab assigned.", this);
            return;
        }

        if (slotTransforms == null || slotTransforms.Length == 0)
        {
            Debug.LogWarning("TileSpawner: no slot transforms assigned.", this);
            return;
        }

        // Destroy previous instances
        if (spawnedTiles != null)
        {
            for (int i = 0; i < spawnedTiles.Length; i++)
            {
                if (spawnedTiles[i] != null)
                {
                    Destroy(spawnedTiles[i]);
                }
            }
        }

        spawnedTiles = new GameObject[slotTransforms.Length];

        for (int i = 0; i < slotTransforms.Length; i++)
        {
            Transform slot = slotTransforms[i];
            if (slot == null)
            {
                continue;
            }

            Transform parent = GetParentForSlot(i);
            GameObject tile = Instantiate(tilePrefab, slot.position, slot.rotation, parent);
            tile.transform.rotation = slot.rotation * Quaternion.Euler(tileRotationEuler);
            tile.transform.localScale = tileScale;

            TileGenerator generator = tile.GetComponent<TileGenerator>();
            if (generator != null)
            {
                generator.TryRandomizeCurrentTile();
            }
            else
            {
                Debug.LogWarning("TileSpawner: tilePrefab has no TileGenerator component.", this);
            }

            spawnedTiles[i] = tile;
        }

        TilesChanged?.Invoke();
    }

    private Transform GetParentForSlot(int slotIndex)
    {
        if (slotIndex == 0 && tile1Parent != null)
        {
            return tile1Parent;
        }

        if (slotIndex == 1 && tile2Parent != null)
        {
            return tile2Parent;
        }

        return spawnParent != null ? spawnParent : transform;
    }

    public GameObject GetSpawnedTile(int index)
    {
        if (spawnedTiles == null || index < 0 || index >= spawnedTiles.Length)
        {
            return null;
        }

        return spawnedTiles[index];
    }

    public bool TryTakeTileData(int index, out MahjongTileData tileData)
    {
        tileData = null;

        GameObject tile = GetSpawnedTile(index);
        if (tile == null)
        {
            return false;
        }

        MahjongTileHolder holder = tile.GetComponent<MahjongTileHolder>();
        if (holder == null || holder.TileData == null)
        {
            return false;
        }

        tileData = CloneTileData(holder.TileData);
        Destroy(tile);
        spawnedTiles[index] = null;
        TilesChanged?.Invoke();
        return true;
    }

    private static MahjongTileData CloneTileData(MahjongTileData source)
    {
        return new MahjongTileData(
            source.TileType,
            source.NumberedValue,
            source.WindValue,
            source.DragonValue,
            source.FlowerValue,
            source.SeasonValue,
            source.Edition);
    }
}
