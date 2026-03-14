using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    private static bool hasHandledInitialSceneLoad;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasHandledInitialSceneLoad = false;
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
