using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

public static class Utils
{
    private static DistributedToggle _defaultToggle;

    /// <summary>
    /// 호출자별로 보관할 균등 인덱스 추첨기를 생성한다.
    /// </summary>
    public static ShuffleBag CreateShuffleBag()
    {
        return new ShuffleBag();
    }

    /// <summary>
    /// 오브젝트를 비추고 있는 카메라 가져오기
    /// </summary>
    /// <returns></returns>
    public static Camera GetMyCamera(GameObject gameObject)
    {
        foreach (Camera camera in Camera.allCameras)
        {
            // 오브젝트 레이어가 카메라의 Culling Mask에 포함되는지 비트 연산으로 확인한다.
            var cullingMask = 1 << gameObject.layer;
            if ((camera.cullingMask & cullingMask) != 0)
                return camera;
        }
        return null;
    }

    /// <summary>
    /// 레이어 마스크 반환
    /// </summary>
    public static int GetLayerMask(eLayer layer)
    {
        return 1 << (int)layer;
    }

    /// <summary>
    /// 공유 판정 이력에 확률을 누적해 성공 여부를 반환한다.
    /// </summary>
    /// <param name="percentNormalize">0~1</param>
    /// <returns>true or false</returns>
    public static bool GetToggleRandom(float percentNormalize)
    {
        if (_defaultToggle == null)
        {
            _defaultToggle = new DistributedToggle();
        }

        return _defaultToggle.Next(percentNormalize);
    }

    /// <summary>
    /// 호출자가 보관하는 독립 판정 이력에 확률을 누적해 성공 여부를 반환한다.
    /// </summary>
    public static bool GetToggleRandom(float percentNormalize, ref DistributedToggle state)
    {
        if (state == null)
        {
            state = new DistributedToggle();
        }

        return state.Next(percentNormalize);
    }

    /// <summary>
    /// 런타임 중 특정 오브젝트 모두 찾기
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> FindAllObjects<T>() where T : UnityEngine.Object
    {
        List<T> objects = new List<T>();
        // 로드된 모든 씬의 루트부터 비활성 자식까지 순회하여 타입이 일치하는 컴포넌트를 모은다.
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                var rootObject = scene.GetRootGameObjects();
                for (int j = 0; j < rootObject.Length; j++)
                {
                    var go = rootObject[j];
                    objects.AddRange(go.GetComponentsInChildren<T>(true));
                }
            }
        }
        return objects;
    }

    /// <summary>
	/// Enum 확장메소드, Description 읽어오기
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static string ToDescription(this Enum source)
    {
        FieldInfo fi = source.GetType().GetField(source.ToString());
        // DescriptionAttribute가 없으면 Enum 멤버 이름을 그대로 사용한다.
        var att = (DescriptionAttribute)fi.GetCustomAttribute(typeof(DescriptionAttribute));
        if (att != null)
        {
            return att.Description;
        }
        else
        {
            return source.ToString();
        }
    }
}
