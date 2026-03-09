using UnityEngine;

public class TitleAnimation : MonoBehaviour
{
    [Header("Tilt Settings")]
    [SerializeField] private float tiltAngle = 15f;
    [SerializeField] private float tiltSpeed = 1f;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Calculate oscillating angle using sine wave
        float angle = Mathf.Sin(Time.time * tiltSpeed) * tiltAngle;
        
        // Apply rotation on Z axis
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
