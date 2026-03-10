using UnityEngine;
using UnityEngine.SceneManagement;

public class MapEncounterResultHandler : MonoBehaviour
{
    [SerializeField] private string mapSceneName = string.Empty;
    [SerializeField] private int mapSceneBuildIndex = -1;

    public void ResolveEncounterWin()
    {
        if (MapRunState.Instance.HasMap)
        {
            MapRunState.Instance.MarkCurrentNodeCleared();
        }
        ReturnToMap();
    }

    public void ResolveEncounterLoss() => ReturnToMap();

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
