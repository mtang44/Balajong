using UnityEngine;

public class MapEncounterResultHandler : MonoBehaviour
{
    [SerializeField] private SceneChanger sceneChanger;
    [SerializeField] private string nextSceneName = string.Empty;
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
                if (sceneChanger != null)
                    sceneChanger.ChangeScene(winSceneName);
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene(winSceneName);
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
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            if (sceneChanger != null)
                sceneChanger.ChangeScene(nextSceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Please set nextSceneName.");
        }
    }
}
