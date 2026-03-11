using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
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
    [Min(0.01f)]
    private float transitionCanvasPlaneDistance = 1f;
    [SerializeField]
    private bool forceTransitionCanvasOnTop = true;
    [SerializeField]
    private int transitionCanvasSortingOrder = 1000;
    [SerializeField]
    private bool forceOpaqueTransitionBackground = true;
    [SerializeField]
    private Color transitionBackgroundColor = Color.black;
    [SerializeField]
    private Image transitionBackgroundImage;

    public void ChangeScene(string sceneName)
    {
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

        canvas.planeDistance = Mathf.Clamp(transitionCanvasPlaneDistance, minDistance, maxDistance);

        if (forceTransitionCanvasOnTop)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = transitionCanvasSortingOrder;
        }

        EnsureOpaqueTransitionBackground(canvas);
    }

    private void EnsureOpaqueTransitionBackground(Canvas canvas)
    {
        if (!forceOpaqueTransitionBackground || canvas == null)
        {
            return;
        }

        Image background = transitionBackgroundImage;
        if (background == null)
        {
            Transform existing = canvas.transform.Find("TransitionBackground");
            if (existing != null)
            {
                background = existing.GetComponent<Image>();
            }
        }

        if (background == null)
        {
            GameObject backgroundObject = new GameObject("TransitionBackground", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(canvas.transform, false);
            background = backgroundObject.GetComponent<Image>();

            RectTransform rect = background.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Keep this behind transition art so it only acts as an opaque blocker.
            background.transform.SetAsFirstSibling();
            transitionBackgroundImage = background;
        }

        background.raycastTarget = false;
        Color color = transitionBackgroundColor;
        color.a = 1f;
        background.color = color;
        background.enabled = true;
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
