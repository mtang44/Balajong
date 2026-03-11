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

    public void ChangeScene(string sceneName)
    {
        if (animator != null)
        {
            if (transitionObject != null)
            {
                transitionObject.SetActive(true);
            }
            animator.SetTrigger("Start");
            StartCoroutine(LoadSceneAfterTransition(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
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
