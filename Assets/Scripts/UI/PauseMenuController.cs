using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject[] activeItems;
    [SerializeField] private GameObject[] deactiveItems;
    [SerializeField] private bool pauseTime = true;

    private bool isOpen;

    private void Start()
    {
        if (activeItems != null)
        {
            foreach (var item in activeItems)
            {
                if (item != null)
                    item.SetActive(false);
            }
        }
        if (deactiveItems != null)
        {
            foreach (var item in deactiveItems)
            {
                if (item != null)
                    item.SetActive(true);
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (activeItems == null || deactiveItems == null) return;

        isOpen = !isOpen;

        foreach (var item in activeItems)
        {
            if (item != null)
                item.SetActive(isOpen);
        }

        foreach (var item in deactiveItems)
        {
            if (item != null)
                item.SetActive(!isOpen);
        }

        if (pauseTime)
            Time.timeScale = isOpen ? 0f : 1f;
    }

    private void OnDisable()
    {
        if (pauseTime)
            Time.timeScale = 1f;
    }
}