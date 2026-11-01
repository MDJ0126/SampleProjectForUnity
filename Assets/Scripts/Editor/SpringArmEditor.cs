using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpringArm))]
public class SpringArmEditor : Editor
{
    /// <summary>
    /// 씬 뷰에서 SpringArm 카메라 위치와 회전 핸들 표시
    /// </summary>
    private void OnSceneGUI()
    {
        SpringArm springArm = (SpringArm)target;

        DrawPositionHandle(springArm);
        DrawRotationHandle(springArm);
    }

    /// <summary>
    /// 월드 위치 핸들의 변경값을 SpringArm 로컬 위치로 저장
    /// </summary>
    private void DrawPositionHandle(SpringArm springArm)
    {
        EditorGUI.BeginChangeCheck();

        Vector3 newPosition = Handles.PositionHandle(
            springArm.CameraWorldPosition,
            springArm.CameraWorldRotation
        );

        if (EditorGUI.EndChangeCheck())
        {
            // Undo를 지원한 뒤 월드 좌표를 부모 기준 로컬 좌표로 변환한다.
            Undo.RecordObject(
                springArm,
                "Move SpringArm Camera"
            );

            springArm.CameraLocalPosition =
                springArm.transform.InverseTransformPoint(newPosition);

            EditorUtility.SetDirty(springArm);
        }
    }

    /// <summary>
    /// 월드 회전 핸들의 변경값을 SpringArm 로컬 회전으로 저장
    /// </summary>
    private void DrawRotationHandle(SpringArm springArm)
    {
        EditorGUI.BeginChangeCheck();

        Quaternion newRotation = Handles.RotationHandle(
            springArm.CameraWorldRotation,
            springArm.CameraWorldPosition
        );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(
                springArm,
                "Rotate SpringArm Camera"
            );

            // 부모 월드 회전의 역회전을 곱해 카메라의 로컬 회전을 구한다.
            Quaternion localRotation =
                Quaternion.Inverse(springArm.transform.rotation)
                * newRotation;

            springArm.CameraLocalRotation =
                localRotation.eulerAngles;

            EditorUtility.SetDirty(springArm);
        }
    }
}
