using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public event Action TransitionStarted;
    public event Action TransitionEnded;

    public static bool IsTransitionInProgress { get; private set; }

    private const string TransitionManagerTag = "TransitionManager";

    private static bool hasHandledInitialSceneLoad;
    private static bool hasPendingTransitionEnd;
    private static bool sceneLoadedHookRegistered;
    private static SceneChanger pendingTransitionSource;

    [SerializeField]
    private Animator animator;
    [SerializeField]
    private float transitionTime = 1f;
    public GameObject transitionObject;
    [SerializeField]
    private TileTransition tileTransition;
    [SerializeField]
    private Canvas transitionCanvas;
    [SerializeField]
    private Camera transitionCanvasCamera;
    [SerializeField]
    private bool autoConfigureTransitionCanvas = true;
    [SerializeField]
    private bool suppressTransitionOnInitialSceneLoad = true;

    public float TransitionDuration => Mathf.Max(0f, transitionTime);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasHandledInitialSceneLoad = false;
        hasPendingTransitionEnd = false;
        pendingTransitionSource = null;
        IsTransitionInProgress = false;

        if (sceneLoadedHookRegistered)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneLoadedHookRegistered = false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHook()
    {
        if (sceneLoadedHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneLoadedHookRegistered = true;
    }

    private static void HandleSceneLoaded(Scene loadedScene, LoadSceneMode loadSceneMode)
    {
        if (!hasPendingTransitionEnd)
        {
            return;
        }

        hasPendingTransitionEnd = false;

        SceneChanger transitionSource = pendingTransitionSource;
        pendingTransitionSource = null;

        if (transitionSource == null)
        {
            try
            {
                GameObject transitionManagerObject = GameObject.FindWithTag(TransitionManagerTag);
                if (transitionManagerObject != null)
                {
                    transitionSource = transitionManagerObject.GetComponent<SceneChanger>();
                }
            }
            catch (UnityException)
            {
            }
        }

        if (transitionSource == null)
        {
            IsTransitionInProgress = false;
            return;
        }

        transitionSource.StartCoroutine(transitionSource.InvokeTransitionEndedAfterDelay());
    }

    private IEnumerator InvokeTransitionEndedAfterDelay()
    {
        float delay = TransitionDuration;
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        IsTransitionInProgress = false;
        TransitionEnded?.Invoke();
    }

    private void Awake()
    {
        if (!hasHandledInitialSceneLoad)
        {
            hasHandledInitialSceneLoad = true;

            if (suppressTransitionOnInitialSceneLoad && transitionObject != null)
            {
                transitionObject.SetActive(false);
            }
        }
    }

    public void ChangeScene(string sceneName)
    {
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        if (BeginTransition())
        {
            StartCoroutine(LoadSceneAfterTransition(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void ChangeScene(int sceneNumber)
    {
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        if (BeginTransition())
        {
            StartCoroutine(LoadSceneAfterTransition(sceneNumber));
        }
        else
        {
            SceneManager.LoadScene(sceneNumber);
        }
    }

    private bool BeginTransition()
    {
        if (animator == null)
        {
            return false;
        }

        if (autoConfigureTransitionCanvas)
        {
            ConfigureTransitionCanvas();
        }

        if (transitionObject != null)
        {
            transitionObject.SetActive(true);
        }

        if (tileTransition != null)
        {
            tileTransition.RandomizeAndSave();
        }

        hasPendingTransitionEnd = true;
        IsTransitionInProgress = true;
        pendingTransitionSource = this;
        TransitionStarted?.Invoke();
        animator.SetTrigger("Start");
        return true;
    }

    private void ConfigureTransitionCanvas()
    {
        Canvas canvas = transitionCanvas;
        if (canvas == null && transitionObject != null)
        {
            canvas = transitionObject.GetComponentInChildren<Canvas>(true);
        }

        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            return;
        }

        Camera resolvedCamera = transitionCanvasCamera != null ? transitionCanvasCamera : Camera.main;
        if (resolvedCamera != null)
        {
            canvas.worldCamera = resolvedCamera;
        }

        Camera activeCamera = canvas.worldCamera;
        if (activeCamera == null)
        {
            return;
        }

        float minDistance = activeCamera.nearClipPlane + 0.01f;
        float maxDistance = activeCamera.farClipPlane - 0.01f;
        if (maxDistance <= minDistance)
        {
            return;
        }
    }

    private IEnumerator LoadSceneAfterTransition(int sceneNumber)
    {
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneNumber);
    }

    private IEnumerator LoadSceneAfterTransition(string sceneName)
    {
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneName);
    }
}
