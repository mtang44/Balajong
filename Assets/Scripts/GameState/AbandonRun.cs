using UnityEngine;
using UnityEngine.SceneManagement;

public class AbandonRun : MonoBehaviour
{
    [SerializeField] private string returnSceneName = "TItle Screen";
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private SceneChanger sceneChanger;

    private void Awake()
    {
        SetConfirmationPanelVisible(false);
    }

    public void OnAbandonRunPressed()
    {
        if (confirmationPanel == null)
        {
            Debug.LogError("AbandonRun: Confirmation panel is not assigned.");
            return;
        }

        SetConfirmationPanelVisible(true);
    }

    public void OnAbandonRunBackPressed()
    {
        SetConfirmationPanelVisible(false);
    }

    public void OnAbandonRunConfirmPressed()
    {
        SetConfirmationPanelVisible(false);
        ConfirmAbandonRun();
    }

    private void ConfirmAbandonRun()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterResetStateFromAbandon();
        }
        else
        {
            Debug.LogWarning("AbandonRun: GameManager instance not found. Loading return scene anyway.");
        }

        if (string.IsNullOrWhiteSpace(returnSceneName))
        {
            Debug.LogError("AbandonRun: returnSceneName is empty.");
            return;
        }

        SceneChanger resolvedSceneChanger = ResolveSceneChanger();
        if (resolvedSceneChanger != null)
        {
            resolvedSceneChanger.ChangeScene(returnSceneName);
            return;
        }

        SceneManager.LoadScene(returnSceneName);
    }

    private void SetConfirmationPanelVisible(bool isVisible)
    {
        if (confirmationPanel == null)
        {
            return;
        }

        if (confirmationPanel.activeSelf != isVisible)
        {
            confirmationPanel.SetActive(isVisible);
        }
    }

    private SceneChanger ResolveSceneChanger()
    {
        if (sceneChanger != null)
        {
            return sceneChanger;
        }

        sceneChanger = FindFirstObjectByType<SceneChanger>(FindObjectsInactive.Include);
        return sceneChanger;
    }
}
