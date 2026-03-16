using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeactivateOnScene : MonoBehaviour
{
    [SerializeField] private List<string> excludedSceneNames = new();

    private void Awake()
    {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        ApplyActiveStateForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        ApplyActiveStateForScene(nextScene.name);
    }

    private void ApplyActiveStateForScene(string sceneName)
    {
        bool shouldBeActive = !IsExcludedScene(sceneName);
        if (gameObject.activeSelf != shouldBeActive)
        {
            gameObject.SetActive(shouldBeActive);
        }
    }

    private bool IsExcludedScene(string sceneName)
    {
        if (excludedSceneNames == null || excludedSceneNames.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < excludedSceneNames.Count; i++)
        {
            if (string.Equals(excludedSceneNames[i], sceneName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
