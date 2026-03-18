using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TextHopEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;

    [Header("Hop")]
    [SerializeField, Min(0f)] private float hopHeight = 18f;
    [SerializeField, Min(0.01f)] private float hopDuration = 0.22f;
    [SerializeField, Min(0f)] private float delayBetweenCharacters = 0.03f;
    [SerializeField] private AnimationCurve hopCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.45f, 1f),
        new Keyframe(1f, 0f));
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine hopRoutine;
    private TMP_MeshInfo[] cachedMeshInfo;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    [ContextMenu("Play Hop")]
    public void PlayHop()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
            if (targetText == null)
            {
                Debug.LogWarning($"{nameof(TextHopEffect)} on {name} needs a TMP_Text reference.", this);
                return;
            }
        }

        if (hopRoutine != null)
        {
            StopCoroutine(hopRoutine);
            RestoreVertices();
        }

        hopRoutine = StartCoroutine(PlayHopRoutine());
    }

    public void StopHop(bool resetVertices = true)
    {
        if (hopRoutine == null)
        {
            return;
        }

        StopCoroutine(hopRoutine);
        hopRoutine = null;

        if (resetVertices)
        {
            RestoreVertices();
        }
    }

    private void OnDisable()
    {
        StopHop(true);
    }

    private IEnumerator PlayHopRoutine()
    {
        targetText.ForceMeshUpdate();
        TMP_TextInfo textInfo = targetText.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0)
        {
            hopRoutine = null;
            yield break;
        }

        cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        float totalDuration = hopDuration + ((characterCount - 1) * delayBetweenCharacters);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            for (int meshIndex = 0; meshIndex < textInfo.meshInfo.Length; meshIndex++)
            {
                Vector3[] sourceVertices = cachedMeshInfo[meshIndex].vertices;
                Vector3[] destinationVertices = textInfo.meshInfo[meshIndex].vertices;
                int copyLength = Mathf.Min(sourceVertices.Length, destinationVertices.Length);
                Array.Copy(sourceVertices, destinationVertices, copyLength);
            }

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                if (!characterInfo.isVisible)
                {
                    continue;
                }

                float localTime = elapsed - (i * delayBetweenCharacters);
                if (localTime < 0f || localTime > hopDuration)
                {
                    continue;
                }

                float normalizedTime = localTime / hopDuration;
                float yOffset = hopCurve.Evaluate(normalizedTime) * hopHeight;
                Vector3 offset = new Vector3(0f, yOffset, 0f);

                int materialIndex = characterInfo.materialReferenceIndex;
                int vertexIndex = characterInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            for (int meshIndex = 0; meshIndex < textInfo.meshInfo.Length; meshIndex++)
            {
                textInfo.meshInfo[meshIndex].mesh.vertices = textInfo.meshInfo[meshIndex].vertices;
                targetText.UpdateGeometry(textInfo.meshInfo[meshIndex].mesh, meshIndex);
            }

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        RestoreVertices();
        hopRoutine = null;
    }

    private void RestoreVertices()
    {
        if (targetText == null || cachedMeshInfo == null)
        {
            return;
        }

        TMP_TextInfo textInfo = targetText.textInfo;
        int meshCount = Mathf.Min(textInfo.meshInfo.Length, cachedMeshInfo.Length);

        for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
        {
            Vector3[] sourceVertices = cachedMeshInfo[meshIndex].vertices;
            Vector3[] destinationVertices = textInfo.meshInfo[meshIndex].vertices;
            int copyLength = Mathf.Min(sourceVertices.Length, destinationVertices.Length);
            Array.Copy(sourceVertices, destinationVertices, copyLength);

            textInfo.meshInfo[meshIndex].mesh.vertices = destinationVertices;
            targetText.UpdateGeometry(textInfo.meshInfo[meshIndex].mesh, meshIndex);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (hopDuration < 0.01f)
        {
            hopDuration = 0.01f;
        }

        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }
#endif
}
