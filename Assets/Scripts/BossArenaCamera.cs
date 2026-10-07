using UnityEngine;
using Unity.Cinemachine;

/// <summary>复用场景唯一的 Cinemachine Camera；战斗时固定取景，结束后恢复原跟随配置。</summary>
[DisallowMultipleComponent]
public class BossArenaCamera : CinemachineExtension
{
    public Camera outputCamera;
    [Tooltip("战斗画面的世界坐标范围；按屏幕比例等比扩大取景，不拉伸画面。")]
    public Rect viewBounds = new Rect(52, -1, 24, 13);
    [Min(0)] public float transitionTime = 0.35f;
    public bool IsLocked { get; private set; }
    Vector3 entryPosition;
    float entryFov, entrySize, elapsed;

    public void SetLocked(bool locked)
    {
        if (locked == IsLocked) return;
        IsLocked = locked;
        if (outputCamera == null) outputCamera = Camera.main;
        if (locked && outputCamera != null)
        {
            entryPosition = outputCamera.transform.position;
            entryFov = outputCamera.fieldOfView;
            entrySize = outputCamera.orthographicSize;
            elapsed = 0;
        }
        else if (ComponentOwner != null) ComponentOwner.PreviousStateIsValid = false;
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (!Application.isPlaying || !IsLocked || stage != CinemachineCore.Stage.Finalize) return;
        float aspect = outputCamera != null ? outputCamera.aspect : 16f / 9;
        float halfHeight = Mathf.Max(viewBounds.height * 0.5f, viewBounds.width * 0.5f / Mathf.Max(0.1f, aspect));
        float distance = Mathf.Max(1, Mathf.Abs(entryPosition.z));
        elapsed += Mathf.Max(0, deltaTime);
        float t = transitionTime <= 0 ? 1 : Mathf.SmoothStep(0, 1, elapsed / transitionTime);
        state.RawPosition = Vector3.Lerp(entryPosition, new Vector3(viewBounds.center.x, viewBounds.center.y, entryPosition.z), t);
        state.PositionCorrection = Vector3.zero;
        state.RawOrientation = Quaternion.identity;
        state.OrientationCorrection = Quaternion.identity;
        var lens = state.Lens;
        lens.OrthographicSize = Mathf.Lerp(entrySize, halfHeight, t);
        lens.FieldOfView = Mathf.Lerp(entryFov, 2 * Mathf.Atan(halfHeight / distance) * Mathf.Rad2Deg, t);
        state.Lens = lens;
    }
}
