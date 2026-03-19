using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public class DisappearOnTransition : MonoBehaviour
{
    private const string TransitionManagerTag = "TransitionManager";
    private const float ZeroScaleThresholdSqr = 0.000001f;

    [SerializeField] private bool useSceneTransitionDuration = true;
    [SerializeField] [Min(0f)] private float defaultScaleDuration = 0.2f;

    private SceneChanger subscribedSceneChanger;
    private Coroutine scaleCoroutine;
    private Vector3 visibleScale;
    private bool waitingForTransitionEnd;

    private void Awake()
    {
        CaptureVisibleScaleIfNonZero();
    }

    private void OnEnable()
    {
        CaptureVisibleScaleIfNonZero();
        RebindSceneChanger();
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (SceneChanger.IsTransitionInProgress)
        {
            waitingForTransitionEnd = true;
            SetScaleInstant(Vector3.zero);
        }
        else
        {
            waitingForTransitionEnd = false;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeFromSceneChanger();
        StopScaleAnimation();
        waitingForTransitionEnd = false;
    }

    private void HandleSceneLoaded(Scene loadedScene, LoadSceneMode loadSceneMode)
    {
        RebindSceneChanger();

        // If scene load callback order caused us to miss TransitionEnded, restore here.
        if (waitingForTransitionEnd && !SceneChanger.IsTransitionInProgress)
        {
            HandleTransitionEnded();
        }
    }

    private void RebindSceneChanger()
    {
        SceneChanger resolvedSceneChanger = ResolveSceneChanger();
        if (resolvedSceneChanger == subscribedSceneChanger)
        {
            return;
        }

        UnsubscribeFromSceneChanger();
        subscribedSceneChanger = resolvedSceneChanger;

        if (subscribedSceneChanger == null)
        {
            return;
        }

        subscribedSceneChanger.TransitionStarted += HandleTransitionStarted;
        subscribedSceneChanger.TransitionEnded += HandleTransitionEnded;
    }

    private SceneChanger ResolveSceneChanger()
    {
        try
        {
            GameObject transitionManagerObject = GameObject.FindWithTag(TransitionManagerTag);
            if (transitionManagerObject != null)
            {
                SceneChanger taggedSceneChanger = transitionManagerObject.GetComponent<SceneChanger>();
                if (taggedSceneChanger != null)
                {
                    return taggedSceneChanger;
                }
            }
        }
        catch (UnityException)
        {
        }

        return FindFirstObjectByType<SceneChanger>();
    }

    private void UnsubscribeFromSceneChanger()
    {
        if (subscribedSceneChanger == null)
        {
            return;
        }

        subscribedSceneChanger.TransitionStarted -= HandleTransitionStarted;
        subscribedSceneChanger.TransitionEnded -= HandleTransitionEnded;
        subscribedSceneChanger = null;
    }

    private void HandleTransitionStarted()
    {
        CaptureVisibleScaleIfNonZero();
        waitingForTransitionEnd = true;
        AnimateScale(Vector3.zero, ResolveDuration());
    }

    private void HandleTransitionEnded()
    {
        waitingForTransitionEnd = false;
        AnimateScale(Vector3.one, ResolveDuration());
    }

    private float ResolveDuration()
    {
        if (useSceneTransitionDuration && subscribedSceneChanger != null)
        {
            return subscribedSceneChanger.TransitionDuration;
        }

        return defaultScaleDuration;
    }

    private void AnimateScale(Vector3 targetScale, float duration)
    {
        StopScaleAnimation();

        if (duration <= 0f)
        {
            SetScaleInstant(targetScale);
            return;
        }

        scaleCoroutine = StartCoroutine(AnimateScaleRoutine(targetScale, duration));
    }

    private IEnumerator AnimateScaleRoutine(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedT);
            yield return null;
        }

        SetScaleInstant(targetScale);
        scaleCoroutine = null;
    }

    private void StopScaleAnimation()
    {
        if (scaleCoroutine == null)
        {
            return;
        }

        StopCoroutine(scaleCoroutine);
        scaleCoroutine = null;
    }

    private void SetScaleInstant(Vector3 scale)
    {
        transform.localRotation = new Quaternion(0f, 0f, 0f, 1f);
        transform.localScale = scale;
    }

    private void CaptureVisibleScaleIfNonZero()
    {
        Vector3 currentScale = transform.localScale;
        if (currentScale.sqrMagnitude > ZeroScaleThresholdSqr)
        {
            visibleScale = currentScale;
        }
    }
}
