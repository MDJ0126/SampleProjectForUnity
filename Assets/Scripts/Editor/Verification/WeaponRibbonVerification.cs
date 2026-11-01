using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Validation
{
    /// <summary>
    /// 실제 검 프리팹의 공격 연동과 잔상 수명 및 월드 궤적을 검증한다.
    /// </summary>
    public static class WeaponRibbonVerification
    {
        [MenuItem("Sample Project/검기 검증")]
        public static void Verify()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "편집 모드에서 검증 실행");
            GameObject instance = null;
            Mesh mesh = null;
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AddressablesResources/Prefabs/Weapon/TestSword.prefab");
                Require(prefab != null, "검 프리팹 로드");
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                MeleeWeapon sword = instance.GetComponent<MeleeWeapon>();
                WeaponRibbon ribbon = instance.GetComponent<WeaponRibbon>();
                Require(sword.weaponRibbon == ribbon && ribbon != null, "프리팹 검기 연결");
                Invoke(ribbon, "Awake");
                mesh = Field<Mesh>(ribbon, "_mesh");
                Require(mesh != null, "검기 초기화");
                Debug.Log("검 메시 bounds: " + ribbon.bladeSource.GetComponent<MeshFilter>().sharedMesh.bounds);
                Debug.Log("검기 로컬 시작/끝: " + Field<Vector3>(ribbon, "_localStart") + " / " + Field<Vector3>(ribbon, "_localEnd"));
                Require(!ShaderUtil.ShaderHasError(ribbon.ribbonMaterial.shader), "검기 셰이더 오류 없음");

                sword.StartAttack();
                Require(!sword.hitBox.enabled && Field<bool>(ribbon, "_isEmitting"), "소유자 없는 편집 미리보기는 피해 판정을 닫고 검기만 시작한다.");
                instance.transform.position += Vector3.right * 0.1f;
                Invoke(ribbon, "LateUpdate");
                Require(mesh.vertexCount == 4 && mesh.triangles.Length == 6, "리본 면 생성");
                Vector3 firstWorld = ribbon.transform.TransformPoint(mesh.vertices[0]);
                instance.transform.position += Vector3.forward * 0.1f;
                Invoke(ribbon, "LateUpdate");
                Require(Vector3.Distance(firstWorld, ribbon.transform.TransformPoint(mesh.vertices[0])) < 0.0001f, "과거 궤적 월드 위치 유지");
                sword.EndAttack();
                Require(!sword.hitBox.enabled && !Field<bool>(ribbon, "_isEmitting"), "공격 종료 연동");
                int vertexCount = mesh.vertexCount;
                instance.transform.position += Vector3.right;
                Invoke(ribbon, "LateUpdate");
                Require(mesh.vertexCount == vertexCount, "종료 후 신규 궤적 생성 없음");
                List<float> times = Field<List<float>>(ribbon, "_times");
                for (int i = 0; i < times.Count; i++)
                {
                    times[i] = Time.time - ribbon.lifetime * 0.5f;
                }
                Invoke(ribbon, "LateUpdate");
                Require(Mathf.Abs(mesh.colors[0].a - ribbon.ribbonColor.a * 0.25f) < 0.001f, "수명 절반에서 알파 감소");
                for (int i = 0; i < times.Count; i++)
                {
                    times[i] = Time.time - ribbon.lifetime - 1f;
                }
                Invoke(ribbon, "LateUpdate");
                Require(mesh.vertexCount == 0, "수명 종료 시 소멸");
                sword.StartAttack();
                instance.transform.position += Vector3.right * 3f;
                Invoke(ribbon, "LateUpdate");
                Require(mesh.vertexCount == 0, "순간이동 시 궤적 끊기");
                ribbon.maxSamples = 4;
                for (int i = 0; i < 12; i++)
                {
                    instance.transform.position += Vector3.right * 0.1f;
                    Invoke(ribbon, "LateUpdate");
                }
                Require(mesh.vertexCount == 8, "정점 수 제한");
                RenderPreview(ribbon);
                instance.SetActive(false);
                Invoke(ribbon, "OnDisable");
                Require(mesh.vertexCount == 0 && !Field<bool>(ribbon, "_isEmitting"), "비활성화 정리");
                Debug.Log("WEAPON_RIBBON_VERIFICATION_PASSED");
            }
            finally
            {
                if (mesh)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                if (instance)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        /// <summary>
        /// 검기만 별도 레이어로 렌더링하여 셰이더 출력을 확인하고 미리보기를 저장한다.
        /// </summary>
        private static void RenderPreview(WeaponRibbon ribbon)
        {
            MeshRenderer renderer = Field<MeshRenderer>(ribbon, "_renderer");
            int originalLayer = renderer.gameObject.layer;
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(512, 512, 24);
            Texture2D image = new Texture2D(512, 512, TextureFormat.RGB24, false);
            GameObject cameraObject = new GameObject("Ribbon Verification Camera");
            try
            {
                Require(ribbon.ribbonMaterial.SetPass(0), "검기 셰이더 패스 생성");
                renderer.gameObject.layer = 31;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.orthographicSize = renderer.bounds.extents.y * 1.2f;
                camera.transform.position = renderer.bounds.center + Vector3.back * 3f;
                camera.transform.rotation = Quaternion.identity;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply();
                int litPixels = 0;
                int magentaPixels = 0;
                foreach (Color32 pixel in image.GetPixels32())
                {
                    if (pixel.g > 30 && pixel.b > 30)
                    {
                        litPixels++;
                    }
                    if (pixel.r > 200 && pixel.b > 200 && pixel.g < 20)
                    {
                        magentaPixels++;
                    }
                }
                Require(litPixels > 100 && magentaPixels == 0, "청백색 검기 렌더링 및 오류 셰이더 없음");
                Require(!ShaderUtil.ShaderHasError(ribbon.ribbonMaterial.shader), "렌더링 후 셰이더 오류 없음");
                System.IO.Directory.CreateDirectory("Library");
                System.IO.File.WriteAllBytes("Library/WeaponRibbonPreview.png", image.EncodeToPNG());
                Debug.Log("검기 렌더링 확인: " + litPixels + " pixels");
            }
            finally
            {
                renderer.gameObject.layer = originalLayer;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(image);
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("검기 검증 실패: " + message);
            }
        }

        private static void Invoke(WeaponRibbon ribbon, string method)
        {
            typeof(WeaponRibbon).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ribbon, null);
        }

        private static T Field<T>(WeaponRibbon ribbon, string field)
        {
            return (T)typeof(WeaponRibbon).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ribbon);
        }
    }
}
