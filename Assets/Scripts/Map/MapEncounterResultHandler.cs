using UnityEngine;
using System.Collections;

public class MapEncounterResultHandler : MonoBehaviour
{
    [SerializeField] private SceneChanger sceneChanger;
    [SerializeField] private string nextSceneName = string.Empty;
    [SerializeField] private string winSceneName = string.Empty;

    [Header("Enemy Defeat Animation")]
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private string enemyDeadBoolParameterName = "EnemyDead";
    [SerializeField] private string enemyShadedBoolParameterName = "Shaded";
    [SerializeField] private bool enemyShadedValueOnDefeat = false;

    [SerializeField] private float delayBeforeReturningToMap = 1.0f;

    private bool warnedMissingEnemyAnimator;
    private bool warnedMissingShadedParameter;

    // Called when the player wins an encounter
    // This tells the map to continue to the next node
    public void ResolveEncounterWin()
    {
        PlayEnemyDefeatAnimation();

        // Reset the deck for the next encounter
        DeckManager.Instance.endRound();
        
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

        StartCoroutine(ReturnToMapAfterDelay());
    }

    // Called when the player loses an encounter
    // This returns the player to the map without advancing the node
    public void ResolveEncounterLoss() => StartCoroutine(ReturnToMapAfterDelay());

    // Returns the player to the map scenes after a delay
    private IEnumerator ReturnToMapAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeReturningToMap);

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

    private void PlayEnemyDefeatAnimation()
    {
        SoundManager.Instance.playKillSound();
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
