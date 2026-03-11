using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChanger : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private float transitionTime = 1f;
    public GameObject transitionObject;

    public void ChangeScene(int sceneNumber)
    {
        if (animator != null)

        {
            // Make sure transition does not play when the game is starting for the first time
            if (transitionObject != null)
            {
                transitionObject.SetActive(true);
            }
            animator.SetTrigger("Start");
            StartCoroutine(LoadSceneAfterTransition(sceneNumber));
        }
        else
        {
            SceneManager.LoadScene(sceneNumber);
        }
    }

    private IEnumerator LoadSceneAfterTransition(int sceneNumber)
    {
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneNumber);
    }
}
