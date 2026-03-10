using UnityEngine;
using UnityEngine.SceneManagement;

public class MapEncounterResultHandler : MonoBehaviour
{
    [SerializeField] private string mapSceneName = string.Empty;
    [SerializeField] private int mapSceneBuildIndex = -1;
    [SerializeField] private string winSceneName = string.Empty;

    // Called when the player wins an encounter
    // This tells the map to continue to the next node
    public void ResolveEncounterWin()
    {
        if (MapRunState.Instance.HasMap)
        {
            MapRunState.Instance.MarkCurrentNodeCleared();
            
            // Check if we just beat the boss - if so, load win scene instead of map
            MapNodeData currentNode = MapRunState.Instance.CurrentMap.FindNodeById(
                MapRunState.Instance.CurrentMap.currentNodeId);
            
            if (currentNode != null && currentNode.type == MapNodeType.Boss && 
                !string.IsNullOrWhiteSpace(winSceneName))
            {
                SceneManager.LoadScene(winSceneName);
                return;
            }
        }
        ReturnToMap();
    }

    // Called when the player loses an encounter
    // This returns the player to the map without advancing the node
    public void ResolveEncounterLoss() => ReturnToMap();

    // Returns the player to the map scenes
    private void ReturnToMap()
    {
        if (mapSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(mapSceneBuildIndex);
        }
        else if (!string.IsNullOrWhiteSpace(mapSceneName))
        {
            SceneManager.LoadScene(mapSceneName);
        }
        else
        {
            Debug.LogWarning("Set mapSceneName or mapSceneBuildIndex to return to map.");
        }
    }
}
