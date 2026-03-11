using UnityEngine;

public class CanvasSetCamera : MonoBehaviour
{
    // Set the canvas to use the canvas camera
    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.worldCamera = GameObject.FindWithTag("CanvasCamera")?.GetComponent<Camera>();
        }
    }
}