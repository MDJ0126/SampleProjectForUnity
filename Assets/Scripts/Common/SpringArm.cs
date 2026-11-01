using System.Collections;
using UnityEngine;

public class SpringArm : MonoBehaviour
{
    #region Inspector

    [SerializeField]
    private Vector3 cameraLocalPosition = new Vector3(0f, 1.5f, -4f);

    [SerializeField]
    private Vector3 cameraLocalRotation = new Vector3(10f, 0f, 0f);

    #endregion

    public Vector3 CameraLocalPosition
    {
        get => cameraLocalPosition;
        set => cameraLocalPosition = value;
    }

    public Vector3 CameraLocalRotation
    {
        get => cameraLocalRotation;
        set => cameraLocalRotation = value;
    }

    public Vector3 CameraWorldPosition => transform.TransformPoint(cameraLocalPosition);

    public Quaternion CameraWorldRotation => transform.rotation * Quaternion.Euler(cameraLocalRotation);

    private PlayerCameraController _playerCameraController;

    /// <summary>
    /// SpringArm에 설정된 초기 카메라 위치/회전을 적용
    /// </summary>
    public void ApplyCameraSetting(PlayerCameraController playerCameraController)
    {
        _playerCameraController = playerCameraController;
        playerCameraController.transform.SetPositionAndRotation(
            CameraWorldPosition,
            CameraWorldRotation
        );
    }

#if UNITY_EDITOR

    /// <summary>
    /// 인스펙터 값 변경 시 실행 상태에 맞춰 카메라 설정 미리보기 갱신
    /// </summary>
    private void OnValidate()
    {
        // 플레이 중에는 현재 이 SpringArm에 연결된 카메라만 다시 바인딩한다.
        if (Application.isPlaying)
        {
            if (_playerCameraController)
            {
                if (_playerCameraController.SpringArm == this)
                {
                    _playerCameraController.Bind(this);
                }
            }
        }
        else
        {
            // 편집 중에는 지연 코루틴으로 직렬화된 값이 반영된 다음 미리보기를 갱신한다.
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayUpdate());
            }
        }

        IEnumerator DelayUpdate()
        {
            yield return null;
            if (this == null || Application.isPlaying || !gameObject.activeInHierarchy)
            {
                yield break;
            }

            // 기본 Pawn의 SpringArm일 때만 메인 카메라 미리보기를 이동한다.
            var parentPawn = GetComponentInParent<Game.Pawn>();
            GameMode mode = GameMode.Instance;
            if (parentPawn != null && mode != null && mode.defaultPawn == parentPawn)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera)
                {
                    mainCamera.transform.SetPositionAndRotation(
                        CameraWorldPosition,
                        CameraWorldRotation
                    );
                }
            }
        }
    }

    /// <summary>
    /// 선택한 SpringArm의 Pivot 연결선과 카메라 방향을 씬 뷰에 표시
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(
            transform.position,
            CameraWorldPosition
        );

        // 카메라의 월드 위치·회전을 기준으로 로컬 기즈모를 그린 뒤 기존 행렬을 복원한다.
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            CameraWorldPosition,
            CameraWorldRotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(0.3f, 0.2f, 0.5f)
        );

        Gizmos.DrawLine(
            Vector3.zero,
            Vector3.forward
        );

        Gizmos.matrix = previousMatrix;
    }

#endif
}
