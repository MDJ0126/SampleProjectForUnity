using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 세 씬을 에디터에서 각각 직접 Play하고 실행 결과를 대조한다.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneFlowEditorVerification
    {
        private const string RUNNING = "SceneFlow.DirectPlay.Running";
        private const string STAGE = "SceneFlow.DirectPlay.Stage";
        private const string INDEX = "SceneFlow.DirectPlay.Index";
        private const string ERRORS = "SceneFlow.DirectPlay.Errors";
        private const string DEADLINE = "SceneFlow.DirectPlay.Deadline";

        [Serializable]
        private class RunResult
        {
            public bool passed;
            public string initialScene;
            public bool editorDirectPlay;
        }

        [Serializable]
        private class FinalResult
        {
            public bool passed;
            public int editorErrors;
            public int scenesVerified;
            public string completedAt;
        }

        static SceneFlowEditorVerification()
        {
            if (SessionState.GetBool(RUNNING, false))
            {
                Hook();
            }
        }

        /// <summary>
        /// 실제 번들을 사용하는 에디터 Play 검증을 배치 실행에서 시작한다.
        /// </summary>
        public static void BuildAndVerify()
        {
            SceneFlowBuild.BuildPlayer();
            VerifyExistingBuild();
        }

        /// <summary>
        /// 이미 생성한 번들로 세 씬의 직접 Play만 다시 검사한다.
        /// </summary>
        public static void VerifyExistingBuild()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-sceneFlowVerify");
            if (index < 0 || index + 1 >= args.Length)
            {
                throw new InvalidOperationException("-sceneFlowVerify 결과 폴더가 필요합니다.");
            }
            SessionState.SetString("SceneFlow.DirectPlay.Output", Path.GetFullPath(args[index + 1]));
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            int packed = settings.DataBuilders.FindIndex(builder => builder != null && builder.GetType().Name == "BuildScriptPackedPlayMode");
            if (packed < 0)
            {
                throw new InvalidOperationException("기존 번들을 사용하는 Play Mode 빌더가 없습니다.");
            }
            settings.ActivePlayModeDataBuilderIndex = packed;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            SessionState.SetBool(RUNNING, true);
            SessionState.SetInt(INDEX, 0);
            SessionState.SetInt(ERRORS, 0);
            SessionState.SetString(STAGE, "start");
            Hook();
        }

        /// <summary>
        /// Domain Reload 이후에도 검사와 오류 관찰을 한 번만 연결한다.
        /// </summary>
        private static void Hook()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        /// <summary>
        /// Play 종료 정리 중 발생한 오류도 전체 결과에 포함한다.
        /// </summary>
        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                SessionState.SetInt(ERRORS, SessionState.GetInt(ERRORS, 0) + 1);
            }
        }

        /// <summary>
        /// 세 씬를 순서대로 직접 열어 Play하고 각 결과 파일을 확인한다.
        /// </summary>
        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
            string stage = SessionState.GetString(STAGE, "start");
            int index = SessionState.GetInt(INDEX, 0);
            string output = SessionState.GetString("SceneFlow.DirectPlay.Output", "");
            if (stage == "start" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (index >= 3)
                {
                    int errors = SessionState.GetInt(ERRORS, 0);
                    Directory.CreateDirectory(output);
                    File.WriteAllText(Path.Combine(output, "editor-results.json"), JsonUtility.ToJson(new FinalResult { passed = errors == 0, editorErrors = errors, scenesVerified = index, completedAt = DateTimeOffset.Now.ToString("O") }, true));
                    SessionState.SetBool(RUNNING, false);
                    EditorApplication.update -= Poll;
                    Application.logMessageReceived -= OnLog;
                    Debug.Log("[SceneFlow Editor QA] " + (errors == 0 ? "PASS" : "FAIL") + " direct Play Splash / Playground / CharacterStage");
                    EditorApplication.Exit(errors == 0 ? 0 : 1);
                    return;
                }
                string id = new[] { "Splash", "Playground", "CharacterStage" }[index];
                EditorSceneManager.playModeStartScene = null;
                EditorSceneManager.OpenScene("Assets/Scenes/" + id + ".unity");
                SessionState.SetFloat(DEADLINE, (float)EditorApplication.timeSinceStartup + 120f);
                SessionState.SetString(STAGE, "playing");
                EditorApplication.isPlaying = true;
            }
            else if (stage == "playing")
            {
                string id = new[] { "Splash", "Playground", "CharacterStage" }[index];
                string resultPath = Path.Combine(output, id, "results.json");
                if (File.Exists(resultPath))
                {
                    RunResult result = JsonUtility.FromJson<RunResult>(File.ReadAllText(resultPath));
                    if (!result.passed || result.initialScene != id || !result.editorDirectPlay)
                    {
                        SessionState.SetInt(ERRORS, SessionState.GetInt(ERRORS, 0) + 1);
                    }
                    SessionState.SetString(STAGE, "stopping");
                    EditorApplication.isPlaying = false;
                }
                else if (EditorApplication.timeSinceStartup > SessionState.GetFloat(DEADLINE, 0))
                {
                    SessionState.SetInt(ERRORS, SessionState.GetInt(ERRORS, 0) + 1);
                    SessionState.SetString(STAGE, "stopping");
                    EditorApplication.isPlaying = false;
                }
            }
            else if (stage == "stopping" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetInt(INDEX, index + 1);
                SessionState.SetString(STAGE, "start");
            }
        }
    }
}
