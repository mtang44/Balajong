using UnityEngine;
using UnityEngine.SceneManagement;

public class MapEncounterResultHandler : MonoBehaviour
{
    [Header("Return Target")]
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

    public void ResolveEncounterLoss()
    {
        ReturnToMap();
    }

    private void ReturnToMap()
    {
        if (mapSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(mapSceneBuildIndex);
            return;
        }

        if (!string.IsNullOrWhiteSpace(mapSceneName))
        {
            SceneManager.LoadScene(mapSceneName);
            return;
        }

        Debug.LogWarning("Please set the mapSceneName or mapSceneBuildIndex to return to the map scene.");
    }
}
