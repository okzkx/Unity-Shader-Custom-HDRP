using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

public class Foreground : CustomPass {
    public float fov = 45;
    public LayerMask foregroundMask;
    Camera foregroundCamera;
    const string kCameraTag = "ForegroundCamera";
    Material depthClearMaterial;
    RTHandle depthBuffer;
    public Vector3 position;
    public Quaternion rotation;

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera) {
        Camera currentCam = hdCamera.camera;
        if (currentCam.cameraType == CameraType.SceneView) {
            return;
        }

        cullingParameters.cullingMask |= (uint) foregroundMask.value;
        
        // 没找到方法能单独设置不对 foregroundMask 层进行视锥剔除
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
        depthClearMaterial = new Material(Shader.Find("Hidden/Renderers/ForegroundDepthClear"));
        var dethBuffer = new RenderTargetIdentifier(BuiltinRenderTextureType.Depth);
        depthBuffer = RTHandles.Alloc(dethBuffer);
    }

    protected override void Execute(CustomPassContext ctx) {
        Camera currentCam = ctx.hdCamera.camera;
        if (currentCam.cameraType == CameraType.SceneView) {
            return;
        }

        if (foregroundCamera == null) {
            CreateForegroundCamera();
        }

        foregroundCamera.transform.SetPositionAndRotation(position, rotation);
        foregroundCamera.fieldOfView = fov;

        // Override depth to 0 (avoid artifacts with screen-space effects)
        ctx.cmd.SetRenderTarget(depthBuffer);
        CustomPassUtils.RenderFromCamera(ctx, foregroundCamera, null, null,
            ClearFlag.None, foregroundMask, overrideMaterial: depthClearMaterial, overrideMaterialIndex: 0);
        // Render the object color
        CustomPassUtils.RenderFromCamera(ctx, foregroundCamera, ctx.cameraColorBuffer, ctx.cameraDepthBuffer,
            ClearFlag.None, foregroundMask);
    }

    private void CreateForegroundCamera() {
        // Hidden foreground camera:
        var cam = GameObject.Find(kCameraTag);
        if (cam == null) {
            // cam = new GameObject(kCameraTag); // For Debugs
            cam = new GameObject(kCameraTag) {hideFlags = HideFlags.HideAndDontSave};
        }

        if (!cam.TryGetComponent<Camera>(out var camera) || camera == null) {
            camera = cam.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = foregroundMask;
        }

        foregroundCamera = camera;
    }

    protected override void Cleanup() {
        depthBuffer.Release();
        CoreUtils.Destroy(depthClearMaterial);
        CoreUtils.Destroy(foregroundCamera.gameObject);
    }
}