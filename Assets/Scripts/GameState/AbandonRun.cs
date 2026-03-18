using UnityEngine;
using UnityEngine.SceneManagement;

public class AbandonRun : MonoBehaviour
{
    private const string JokerCanvasTag = "JokerCanvas";

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
        // Clear all joker visuals from shop
        ClearJokerVisuals();
        DestroyJokerCanvasObjects();

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

    private void ClearJokerVisuals()
    {
        ShopPurchase shopPurchase = FindFirstObjectByType<ShopPurchase>(FindObjectsInactive.Include);
        if (shopPurchase != null && shopPurchase.jokerPanels != null)
        {
            foreach (GameObject panel in shopPurchase.jokerPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(true);
                }
            }
        }
    }

    private void DestroyJokerCanvasObjects()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject currentObject = allObjects[i];
            if (currentObject == null || currentObject.tag != JokerCanvasTag)
            {
                continue;
            }

            Destroy(currentObject);
        }
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
