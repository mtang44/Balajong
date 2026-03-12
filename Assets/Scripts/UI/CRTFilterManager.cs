using UnityEngine;
using UnityEngine.Rendering.Universal;

// Extends URP's built-in fullscreen feature and skips overlay cameras to avoid effect stacking.
public class CRTFilterRenderFeature : FullScreenPassRendererFeature
{
    [SerializeField] private bool applyToOverlayCameras = false;
    [SerializeField] private bool applyToSceneView = false;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        ref CameraData cameraData = ref renderingData.cameraData;

        if (!applyToOverlayCameras && cameraData.renderType == CameraRenderType.Overlay)
        {
            return;
        }

        if (!applyToSceneView && cameraData.cameraType == CameraType.SceneView)
        {
            return;
        }

        base.AddRenderPasses(renderer, ref renderingData);
    }
}