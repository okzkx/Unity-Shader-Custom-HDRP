using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

public class OverrideCamera : CustomPass {
    public float fov = 45;
    public LayerMask overrideMask;
    Camera overrideCamera;
    const string kCameraTag = "OverrideCamera";
    Material depthClearMaterial;
    RTHandle depthBuffer;
    public Vector3 position;
    public Quaternion rotation;

    protected override bool executeInSceneView => false;

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera) {
        Camera currentCam = hdCamera.camera;
        if (currentCam.cameraType == CameraType.SceneView) {
            return;
        }

        cullingParameters.cullingMask |= (uint) overrideMask.value;

        // 没找到方法能单独设置不对 overrideMask 层进行视锥剔除
        // 临时方案是让物体始终在 MainCamera 前方，或者调大 Bounding Box，以至于不被主相机剔除掉
        // Doesn't work 
        // cullingParameters.isOrthographic = true;
        // cullingParameters.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
        //                                   Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
        //                                   hdCamera.camera.worldToCameraMatrix;
        // Doesn't work either
        // cullingParameters.cullingOptions = CullingOptions.None;
    }

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) {
        depthClearMaterial = new Material(Shader.Find("Hidden/Renderers/OverrideDepthClear"));
        var dethBuffer = new RenderTargetIdentifier(BuiltinRenderTextureType.Depth);
        depthBuffer = RTHandles.Alloc(dethBuffer);
    }

    protected override void Execute(CustomPassContext ctx) {
        Camera currentCam = ctx.hdCamera.camera;
        if (currentCam.cameraType == CameraType.SceneView) {
            return;
        }

        if (overrideCamera == null) {
            CreateOverrideCamera();
        }

        overrideCamera.transform.SetPositionAndRotation(position, rotation);
        overrideCamera.fieldOfView = fov;

        // Use overrideCamera's cullingParameter to override CurrentCam's Culling result
        if (overrideCamera.TryGetCullingParameters(out var cullingParameters)) {
            cullingParameters.cullingOptions = CullingOptions.None;
            ctx.cullingResults = ctx.renderContext.Cull(ref cullingParameters);
        }

        // Override depth to 0 (avoid artifacts with screen-space effects)
        ctx.cmd.SetRenderTarget(depthBuffer);
        CustomPassUtils.RenderFromCamera(ctx, overrideCamera, null, null,
            ClearFlag.None, overrideMask, overrideMaterial: depthClearMaterial, overrideMaterialIndex: 0);
        // Render the object color
        CustomPassUtils.RenderFromCamera(ctx, overrideCamera, ctx.cameraColorBuffer, ctx.cameraDepthBuffer,
            ClearFlag.None, overrideMask);
    }

    private void CreateOverrideCamera() {
        // Hidden override camera:
        var cam = GameObject.Find(kCameraTag);
        if (cam == null) {
            // cam = new GameObject(kCameraTag); // For Debugs
            cam = new GameObject(kCameraTag) {hideFlags = HideFlags.HideAndDontSave};
        }

        if (!cam.TryGetComponent<Camera>(out var camera) || camera == null) {
            camera = cam.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = overrideMask;
        }

        overrideCamera = camera;
    }

    protected override void Cleanup() {
        depthBuffer.Release();
        CoreUtils.Destroy(depthClearMaterial);
        CoreUtils.Destroy(overrideCamera.gameObject);
    }
}