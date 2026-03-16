using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeactivateOnObjectActivate : MonoBehaviour
{
    [SerializeField] private List<string> watchedTags = new();

    private static readonly List<DeactivateOnObjectActivate> registeredControllers = new();
    private static RuntimeUpdater runtimeUpdater;
    private readonly HashSet<string> warnedInvalidTags = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        runtimeUpdater = null;
        registeredControllers.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeAfterSceneLoad()
    {
        EnsureRuntimeUpdater();
        RegisterExistingControllers();
    }

    private void Awake()
    {
        EnsureRuntimeUpdater();
        RegisterController(this);
        ApplyActiveState();
    }

    private void OnDestroy()
    {
        registeredControllers.Remove(this);
    }

    private static void EnsureRuntimeUpdater()
    {
        if (runtimeUpdater != null)
        {
            return;
        }

        GameObject updaterObject = new GameObject(nameof(DeactivateOnObjectActivate) + "RuntimeUpdater");
        updaterObject.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(updaterObject);
        runtimeUpdater = updaterObject.AddComponent<RuntimeUpdater>();
    }

    private static void RegisterExistingControllers()
    {
        DeactivateOnObjectActivate[] foundControllers = Resources.FindObjectsOfTypeAll<DeactivateOnObjectActivate>();
        for (int i = 0; i < foundControllers.Length; i++)
        {
            RegisterController(foundControllers[i]);
        }
    }

    private static void RegisterController(DeactivateOnObjectActivate controller)
    {
        if (controller == null || registeredControllers.Contains(controller))
        {
            return;
        }

        GameObject controllerObject = controller.gameObject;
        if (!controllerObject.scene.IsValid() || !controllerObject.scene.isLoaded)
        {
            return;
        }

        registeredControllers.Add(controller);
    }

    private bool ShouldBeActive()
    {
        if (watchedTags == null || watchedTags.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < watchedTags.Count; i++)
        {
            string tagName = watchedTags[i];
            if (string.IsNullOrWhiteSpace(tagName))
            {
                continue;
            }

            if (HasAnyOtherActiveObjectWithTag(tagName))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasAnyOtherActiveObjectWithTag(string tagName)
    {
        try
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tagName);
            for (int i = 0; i < taggedObjects.Length; i++)
            {
                GameObject taggedObject = taggedObjects[i];
                if (taggedObject != null && taggedObject != gameObject)
                {
                    return true;
                }
            }
        }
        catch (UnityException)
        {
            if (warnedInvalidTags.Add(tagName))
            {
                Debug.LogWarning($"DeactivateOnObjectActivate on '{name}' references undefined tag '{tagName}'.", this);
            }
        }

        return false;
    }

    private void ApplyActiveState()
    {
        bool shouldBeActive = ShouldBeActive();
        if (gameObject.activeSelf != shouldBeActive)
        {
            gameObject.SetActive(shouldBeActive);
        }
    }

    private sealed class RuntimeUpdater : MonoBehaviour
    {
        private const int RescanFrameInterval = 30;

        private int nextRescanFrame;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (Time.frameCount >= nextRescanFrame)
            {
                RegisterExistingControllers();
                nextRescanFrame = Time.frameCount + RescanFrameInterval;
            }

            for (int i = registeredControllers.Count - 1; i >= 0; i--)
            {
                DeactivateOnObjectActivate controller = registeredControllers[i];
                if (controller == null)
                {
                    registeredControllers.RemoveAt(i);
                    continue;
                }

                GameObject controllerObject = controller.gameObject;
                if (!controllerObject.scene.IsValid() || !controllerObject.scene.isLoaded)
                {
                    registeredControllers.RemoveAt(i);
                    continue;
                }

                if (!controller.enabled)
                {
                    continue;
                }

                controller.ApplyActiveState();
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            RegisterExistingControllers();
        }
    }
}
