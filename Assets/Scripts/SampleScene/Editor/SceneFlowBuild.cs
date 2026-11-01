using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 저장된 씬·프리팹을 수정하지 않고 실제 Addressables 플레이어를 빌드한다.
    /// </summary>
    public static class SceneFlowBuild
    {
        public const string PLAYER = "Builds/SceneFlow/SceneFlow.exe";

        /// <summary>
        /// 기본 시작 씬인 Splash를 직접 연다.
        /// </summary>
        [MenuItem("Sample Project/Scene Flow/Open Splash")]
        public static void OpenSplash()
        {
            OpenScene("Splash");
        }

        /// <summary>
        /// 기존 Playground에서 직접 재생할 수 있게 연다.
        /// </summary>
        [MenuItem("Sample Project/Scene Flow/Open Playground")]
        public static void OpenPlayground()
        {
            OpenScene("Playground");
        }

        /// <summary>
        /// 장비·코스튬용 정면 캐릭터 스테이지를 연다.
        /// </summary>
        [MenuItem("Sample Project/Scene Flow/Open Character Stage")]
        public static void OpenCharacterStage()
        {
            OpenScene("CharacterStage");
        }

        /// <summary>
        /// 현재 저장된 오브젝트 구성을 그대로 콘텐츠와 플레이어로 빌드한다.
        /// </summary>
        [MenuItem("Sample Project/Scene Flow/Build Player")]
        public static void BuildPlayer()
        {
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult content);
            if (!string.IsNullOrEmpty(content.Error))
            {
                throw new InvalidOperationException(content.Error);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(PLAYER));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Splash.unity" },
                locationPathName = PLAYER,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("씬 흐름 플레이어 빌드 실패: " + report.summary.result);
            }
            Debug.Log("[SceneFlow] Player build succeeded");
        }

        /// <summary>
        /// 미저장 씬을 보존하고 시작 씬 강제 설정 없이 지정한 씬을 연다.
        /// </summary>
        private static void OpenScene(string id)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.playModeStartScene = null;
                EditorSceneManager.OpenScene("Assets/Scenes/" + id + ".unity");
            }
        }
    }
}
