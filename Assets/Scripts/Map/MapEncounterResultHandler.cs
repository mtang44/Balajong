using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class MapEncounterResultHandler : MonoBehaviour
{
    [SerializeField] private SceneChanger sceneChanger;
    [SerializeField] private string nextSceneName = string.Empty;
    [SerializeField] private string winSceneName = string.Empty;

    [Header("Battle Win Feedback")]
    [SerializeField] private GameObject battleWin;
    [SerializeField] private TextHopEffect battleWinTextHopEffect;

    [Header("Boss Win Feedback")]
    [SerializeField] private GameObject bossWin;
    [SerializeField] private TextHopEffect bossWinTextHopEffect;

    [Header("Enemy Defeat Animation")]
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private string enemyDeadBoolParameterName = "EnemyDead";
    [SerializeField] private string enemyShadedBoolParameterName = "Shaded";
    [SerializeField] private bool enemyShadedValueOnDefeat = false;

    [Header("Scene Transition")]
    [SerializeField, FormerlySerializedAs("delayBeforeReturningToMap"), Min(0f)]
    private float delayBeforeSceneTransition = 1.0f;

    private bool warnedMissingEnemyAnimator;
    private bool warnedMissingShadedParameter;
    private Coroutine transitionRoutine;

    // Called when the player wins an encounter
    // This tells the map to continue to the next node
    public void ResolveEncounterWin()
    {
        PlayEnemyDefeatAnimation();

        PlayerStatManager.Instance?.RecordEnemyDefeated();

        // Reset the deck for the next encounter
        DeckManager.Instance.endRound();

        bool isBossEncounter = IsCurrentEncounterBoss();

        if (MapRunState.Instance.HasMap)
        {
            MapRunState.Instance.MarkCurrentNodeCleared();
        }

        if (!isBossEncounter)
        {
            ShowBattleWinFeedback();
            StartSceneTransitionAfterDelay(nextSceneName);
        }
        else
        {
            SetBattleWinActive(false);
            ShowBossWinFeedback();

            // If no boss win overlay is assigned, fall back to auto-transition
            if (bossWin == null)
            {
                string targetSceneName = !string.IsNullOrWhiteSpace(winSceneName) ? winSceneName : nextSceneName;
                StartSceneTransitionAfterDelay(targetSceneName);
            }
        }
    }

    // Called when the player loses an encounter
    // This returns the player to the map without advancing the node
    public void ResolveEncounterLoss()
    {
        DeckManager.Instance.endRound();
        SetBattleWinActive(false);
        SetBossWinActive(false);
        StartSceneTransitionAfterDelay(nextSceneName);
    }

    private bool IsCurrentEncounterBoss()
    {
        if (!MapRunState.Instance.HasMap)
        {
            return false;
        }

        NodeMapData currentMap = MapRunState.Instance.CurrentMap;
        if (currentMap == null)
        {
            return false;
        }

        MapNodeData currentNode = currentMap.FindNodeById(currentMap.currentNodeId);
        return currentNode != null && currentNode.type == MapNodeType.Boss;
    }

    private void ShowBattleWinFeedback()
    {
        if (battleWin == null)
        {
            return;
        }

        SetBattleWinActive(true);

        if (battleWinTextHopEffect == null)
        {
            battleWinTextHopEffect = battleWin.GetComponentInChildren<TextHopEffect>(true);
        }

        if (battleWinTextHopEffect != null)
        {
            battleWinTextHopEffect.PlayHop();
        }
    }

    private void SetBattleWinActive(bool shouldBeActive)
    {
        if (battleWin != null && battleWin.activeSelf != shouldBeActive)
        {
            battleWin.SetActive(shouldBeActive);
        }
    }

    private void ShowBossWinFeedback()
    {
        if (bossWin == null)
        {
            return;
        }

        SetBossWinActive(true);

        if (bossWinTextHopEffect == null)
        {
            bossWinTextHopEffect = bossWin.GetComponentInChildren<TextHopEffect>(true);
        }

        if (bossWinTextHopEffect != null)
        {
            bossWinTextHopEffect.PlayHop();
        }
    }

    private void SetBossWinActive(bool shouldBeActive)
    {
        if (bossWin != null && bossWin.activeSelf != shouldBeActive)
        {
            bossWin.SetActive(shouldBeActive);
        }
    }

    // Called by a UI button to loop the run: resets the map and returns the player to the start
    public void LoopRun()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        MapRunState.Instance.IncrementLoop();
        MapRunState.Instance.ClearMap();

        if (sceneChanger != null)
            sceneChanger.ChangeScene(nextSceneName);
        else
            SceneManager.LoadScene(nextSceneName);
    }

    private void StartSceneTransitionAfterDelay(string sceneName)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(TransitionAfterDelay(sceneName));
    }

    // Returns the player to the target scene after a delay
    private IEnumerator TransitionAfterDelay(string sceneName)
    {
        if (delayBeforeSceneTransition > 0f)
        {
            yield return new WaitForSeconds(delayBeforeSceneTransition);
        }

        transitionRoutine = null;

        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            if (sceneChanger != null)
                sceneChanger.ChangeScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("MapEncounterResultHandler: Please set a valid target scene name.");
        }
    }

    private void PlayEnemyDefeatAnimation()
    {
        if (enemyAnimator == null && EnemyManager.Instance != null)
        {
            enemyAnimator = EnemyManager.Instance.GetComponentInChildren<Animator>(true);
        }

        if (enemyAnimator == null)
        {
            if (!warnedMissingEnemyAnimator)
            {
                warnedMissingEnemyAnimator = true;
                Debug.LogWarning("MapEncounterResultHandler: Enemy animator is not assigned. Enemy dead animation bool was not set.");
            }

            return;
        }

        ApplyEnemyShadedVariant();

        if (string.IsNullOrWhiteSpace(enemyDeadBoolParameterName))
        {
            return;
        }

        if (HasBoolParameter(enemyAnimator, enemyDeadBoolParameterName))
        {
            enemyAnimator.SetBool(enemyDeadBoolParameterName, true);
            return;
        }

        Debug.LogWarning($"MapEncounterResultHandler: Could not find bool parameter '{enemyDeadBoolParameterName}' on enemy animator.");
    }

    private void ApplyEnemyShadedVariant()
    {
        if (string.IsNullOrWhiteSpace(enemyShadedBoolParameterName))
        {
            return;
        }

        if (HasBoolParameter(enemyAnimator, enemyShadedBoolParameterName))
        {
            enemyAnimator.SetBool(enemyShadedBoolParameterName, enemyShadedValueOnDefeat);
            return;
        }

        if (!warnedMissingShadedParameter)
        {
            warnedMissingShadedParameter = true;
            Debug.LogWarning($"MapEncounterResultHandler: Could not find bool parameter '{enemyShadedBoolParameterName}' on enemy animator.");
        }
    }

    private static bool HasBoolParameter(Animator animator, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }
}
