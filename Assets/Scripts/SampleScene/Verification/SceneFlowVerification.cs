using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 명시적인 검사 인자로 실행할 때 실제 씬 흐름과 배치 UI를 검증한다.
    /// </summary>
    public class SceneFlowVerification : MonoBehaviour
    {
        [Serializable]
        private class Result
        {
            public string startedAt;
            public string completedAt;
            public string initialScene;
            public bool editorDirectPlay;
            public bool passed;
            public int screenWidth;
            public int screenHeight;
            public List<string> buttonRaycasts = new List<string>();
            public List<string> checks = new List<string>();
            public List<string> managerSnapshots = new List<string>();
            public List<string> errors = new List<string>();
        }
        private readonly Result _result = new Result();
        private string _output;
        private int _initialHandle;
        private SceneUIInitializer _initializer;
        private SceneControlManager _control;
        private UIManager _ui;

        private async void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-sceneFlowVerify");
            if (index < 0 || index + 1 >= args.Length)
            {
                return;
            }
            _result.startedAt = DateTimeOffset.Now.ToString("O");
            _result.initialScene = SceneManager.GetActiveScene().name;
            _result.editorDirectPlay = Application.isEditor;
            _result.screenWidth = Screen.width;
            _result.screenHeight = Screen.height;
            _initialHandle = SceneManager.GetActiveScene().handle;
            _output = Path.Combine(Path.GetFullPath(args[index + 1]), _result.initialScene);
            Directory.CreateDirectory(_output);
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            Application.logMessageReceived += OnLog;
            try
            {
                await RunAsync();
                _result.passed = _result.errors.Count == 0;
            }
            catch (Exception exception)
            {
                _result.errors.Add(exception.ToString());
                Debug.LogError("[SceneFlow QA] " + exception);
            }
            finally
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Application.logMessageReceived -= OnLog;
                _result.completedAt = DateTimeOffset.Now.ToString("O");
                File.WriteAllText(Path.Combine(_output, "results.json"), JsonUtility.ToJson(_result, true));
                Debug.Log("[SceneFlow QA] " + (_result.passed ? "PASS" : "FAIL") + " / " + _result.checks.Count);
                if (!Application.isEditor)
                {
                    Application.Quit(_result.passed ? 0 : 1);
                }
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.logMessageReceived -= OnLog;
        }

        /// <summary>
        /// 시작 경로·직렬화된 버튼·플레이 입력·스테이지·복구를 실제 실행한다.
        /// </summary>
        private async Task RunAsync()
        {
            await WaitAsync(() => SceneControlManager.Instance != null && !SceneControlManager.Instance.IsTransitioning && SceneControlManager.Instance.CurrentSceneId == _result.initialScene, "native scene adoption");
            _initializer = SceneUIInitializer.Instance;
            _control = SceneControlManager.Instance;
            _ui = UIManager.Instance;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Require(SceneManager.GetActiveScene().handle == _initialHandle, "Direct Play keeps the originally opened scene");
            CheckScene(_result.initialScene);
            Require(await SceneUIInitializer.EnsureAsync(null) == _initializer, "Initialization reuses existing prefab instance");
            if (_result.initialScene == "Splash")
            {
                await CaptureAsync("splash.png");
                await WaitAsync(() => !_control.IsTransitioning && _control.CurrentSceneId == "Playground", "automatic Splash to Playground");
                Require(SceneManager.GetSceneByName("Splash").isLoaded == false, "Splash is released after automatic entry");
            }
            else if (_result.initialScene == "CharacterStage")
            {
                CheckStage();
                await CaptureAsync("direct-stage.png");
                Click("ReturnButton");
                await WaitSceneAsync("Playground");
            }
            CheckScene("Playground");
            SceneFlowContent playground = Content();
            await WaitAsync(() => playground.playerController.IsPossessed, "existing Playground Pawn possession");
            Require(playground.gameplayCharacter != null && playground.IsMenuOpen && !playground.playerInput.enabled, "Existing character retained with clickable menu input");
            await CaptureAsync("playground.png");
            Click("MenuPopupButton");
            await WaitAsync(() => _ui.Find("MenuPopup") != null, "serialized popup button");
            Require(_ui.IsGameplayInputBlocked, "Modal owns gameplay input blocking");
            await CaptureAsync("popup.png");
            PopupUIView popup = (PopupUIView)_ui.Find("MenuPopup");
            ClickButton(popup.closeButton);
            await WaitAsync(() => _ui.Find("MenuPopup") == null && !_ui.IsGameplayInputBlocked, "serialized close event releases popup");
            Click("ResumeButton");
            await WaitAsync(() => !playground.IsMenuOpen && playground.playerInput.enabled, "resume original player input");
            Require(playground.playerController.enabled && playground.gameplayCameraController.enabled, "Gameplay enables the existing player and mouse camera controllers");
            if (!Application.isEditor || !Application.isBatchMode)
            {
                Require(Cursor.lockState == CursorLockMode.Locked, "Gameplay captures pointer for the existing camera controller");
            }
            await _ui.OpenAsync("MenuPopup");
            Require(!playground.playerInput.enabled && Cursor.lockState == CursorLockMode.None, "Modal stops actual player input and frees pointer");
            await PressAsync(Key.Escape, () => _ui.Find("MenuPopup") == null);
            Require(playground.playerInput.enabled && !_ui.IsGameplayInputBlocked, "Escape releases modal and restores gameplay input");
            await PressAsync(Key.Tab, () => playground.IsMenuOpen);
            Require(!playground.playerInput.enabled && Cursor.lockState == CursorLockMode.None, "Tab opens clickable menu and stops gameplay");
            await WaitAsync(() => ((SceneFlowView)_ui.Find("PlaygroundScreen")).gameplayMenu.activeInHierarchy, "authored menu becomes visible after Tab");
            Click("CharacterStageButton");
            await WaitAsync(() => _ui.Find("Loading") != null, "loading overlay");
            Require(_ui.IsGameplayInputBlocked, "Loading blocks gameplay");
            await CaptureAsync("loading.png");
            await WaitSceneAsync("CharacterStage");
            CheckStage();
            await CaptureAsync("character-stage.png");
            Click("CostumeTab");
            SceneFlowView stageView = (SceneFlowView)_ui.Find("CharacterStageScreen");
            Require(stageView.costumePanel.activeSelf && !stageView.equipmentPanel.activeSelf, "Inspector int event switches to authored costume panel");
            await CaptureAsync("costume.png");
            Click("EquipmentTab");
            Require(stageView.equipmentPanel.activeSelf && !stageView.costumePanel.activeSelf, "Inspector int event switches to authored equipment panel");
            Click("ReturnButton");
            await WaitSceneAsync("Playground");
            await WaitAsync(() => Content().playerController.IsPossessed, "return Pawn possession");
            Require(!await _control.ChangeSceneAsync("MissingScene"), "Unknown scene rejected");
            Require(!await _control.ChangeSceneAsync("Playground"), "Same scene rejected");
            SceneDefinition target = _control.catalog.Find("CharacterStage");
            string screen = target.screenId;
            target.screenId = "MissingScreen";
            bool failed;
            try
            {
                failed = !await _control.ChangeSceneAsync("CharacterStage");
            }
            finally
            {
                target.screenId = screen;
            }
            Require(failed && _control.CurrentSceneId == "Playground" && SceneManager.GetActiveScene().name == "Playground", "Failed destination screen restores prior scene");
            await WaitAsync(() => _ui.Find("CharacterStageScreen") == null, "failed native screen unregister");
            Require(_ui.Find("Error") != null && _ui.Find("Loading") == null, "Failure closes loading and presents error popup");
            await CaptureAsync("recovery.png");
            ClickButton(((PopupUIView)_ui.Find("Error")).closeButton);
            for (int repeat = 0; repeat < 6; repeat++)
            {
                string id = _control.CurrentSceneId == "Playground" ? "CharacterStage" : "Playground";
                Task<bool> first = _control.ChangeSceneAsync(id);
                Require(!await _control.ChangeSceneAsync(id), "Concurrent transition rejected " + repeat);
                Require(await first, "Repeated scene transition succeeds " + repeat);
                CheckScene(id);
            }
            await WaitAsync(() => _ui.InFlightCount == 0, "UI operations finish");
            Require(_ui.OpenCount == 1 && _ui.PendingCount == 0, "Only one authored screen remains after all transitions");
            CheckManagers("final");
        }

        /// <summary>
        /// 정면 캐릭터·카메라 구도·실제 렌더러·유휴 애니메이션을 검사한다.
        /// </summary>
        private void CheckStage()
        {
            CheckScene("CharacterStage");
            SceneFlowContent content = Content();
            Vector3 toCamera = Vector3.ProjectOnPlane(content.sceneCamera.transform.position - content.previewCharacter.position, Vector3.up).normalized;
            Require(Vector3.Dot(content.previewCharacter.forward, toCamera) > 0.99f, "Character faces the preview camera");
            Animator animator = content.previewCharacter.GetComponentInChildren<Animator>();
            Require(animator != null && animator.isHuman && animator.runtimeAnimatorController != null, "Stage uses an isolated humanoid idle Animator");
            Renderer[] renderers = content.previewCharacter.GetComponentsInChildren<Renderer>();
            Require(renderers.Length > 0 && Array.Exists(renderers, renderer => renderer.enabled), "Actual Robot Kyle renderers exist in the stage");
            Vector3 center = content.sceneCamera.WorldToViewportPoint(content.previewCharacter.position + Vector3.up);
            Require(center.z > 0f && center.x > 0.25f && center.x < 0.75f && center.y > 0f && center.y < 1f, "Character is framed between the authored UI side panels");
            Require(content.playerController == null && Cursor.lockState == CursorLockMode.None, "Preview scene keeps gameplay controller out and pointer available");
        }

        /// <summary>
        /// 배치된 화면을 그대로 관리하고 이전 씬을 정리했는지 확인한다.
        /// </summary>
        private void CheckScene(string id)
        {
            Require(_control.CurrentSceneId == id && SceneManager.GetActiveScene().name == id && SceneManager.sceneCount == 1, "Single active gameplay scene: " + id);
            SceneFlowContent content = Content();
            Require(content.screenView != null && _ui.Find(id + "Screen") == content.screenView && content.screenView.gameObject.scene == content.gameObject.scene, "Manager adopts the screen already authored in scene: " + id);
            Require(_ui.OpenCount == 1 && _ui.Find("Loading") == null, "Screen and loading lifetime stable: " + id);
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Single scene-owned AudioListener: " + id);
            CheckManagers(id);
        }

        /// <summary>
        /// 비활성 인스턴스까지 포함하여 누적 생성과 객체 동일성을 확인한다.
        /// </summary>
        private void CheckManagers(string phase)
        {
            int roots = UnityEngine.Object.FindObjectsByType<SceneUIInitializer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int controls = UnityEngine.Object.FindObjectsByType<SceneControlManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int ui = UnityEngine.Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int events = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            _result.managerSnapshots.Add(phase + ": roots=" + roots + ", controls=" + controls + ", ui=" + ui + ", events=" + events + ", created=" + SceneUIInitializer.CreatedRootCount);
            Require(roots == 1 && controls == 1 && ui == 1 && events == 1 && SceneUIInitializer.CreatedRootCount == 1 && _initializer == SceneUIInitializer.Instance && _control == SceneControlManager.Instance && _ui == UIManager.Instance, "Single persistent manager identities: " + phase);
        }

        /// <summary>
        /// 씬 로드 직후에도 일시적인 중복 매니저가 생기지 않는지 관찰한다.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckManagers("sceneLoaded " + scene.name);
        }

        /// <summary>
        /// 현재 씬이 가진 진입·화면·입력 참조를 조회한다.
        /// </summary>
        private static SceneFlowContent Content()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                SceneFlowContent content = root.GetComponentInChildren<SceneFlowContent>();
                if (content != null)
                {
                    return content;
                }
            }
            throw new InvalidOperationException("SceneFlowContent missing");
        }

        /// <summary>
        /// 프리팹에 미리 배치된 이름의 버튼을 찾아 실제 이벤트 경로로 누른다.
        /// </summary>
        private void Click(string name)
        {
            foreach (Button button in _ui.Find(_control.CurrentSceneId + "Screen").GetComponentsInChildren<Button>())
            {
                if (button.name == name)
                {
                    ClickButton(button);
                    return;
                }
            }
            throw new InvalidOperationException("Authored button missing: " + name);
        }

        /// <summary>
        /// EventSystem 레이캐스트와 직렬화된 UnityEvent 대상·메서드를 검사한다.
        /// </summary>
        private void ClickButton(Button button)
        {
            Canvas.ForceUpdateCanvases();
            Require(button.onClick.GetPersistentEventCount() > 0 && button.onClick.GetPersistentTarget(0) != null && !string.IsNullOrEmpty(button.onClick.GetPersistentMethodName(0)), "Persistent Inspector button event: " + button.name);
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            _result.buttonRaycasts.Add(button.name + ": position=" + pointer.position + ", screen=" + Screen.width + "x" + Screen.height + ", hits=" + hits.Count + ", first=" + (hits.Count > 0 ? hits[0].gameObject.name : "none"));
            Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject, "EventSystem raycast reaches authored button: " + button.name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>
        /// 이전 Play와 실제 키보드 상태를 분리한 장치로 입력을 큐에 전달한다.
        /// </summary>
        private async Task PressAsync(Key key, Func<bool> expected)
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>("SceneFlowVerificationKeyboard");
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                await WaitAsync(expected, "queued key " + key);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await Task.Yield();
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        /// <summary>
        /// 전환 잠금과 목적 씬 확정이 모두 끝날 때까지 기다린다.
        /// </summary>
        private Task WaitSceneAsync(string id)
        {
            return WaitAsync(() => !_control.IsTransitioning && _control.CurrentSceneId == id, "scene " + id);
        }

        /// <summary>
        /// 종료 조건을 실제 프레임마다 관찰하고 시간 초과는 실패로 기록한다.
        /// </summary>
        private async Task WaitAsync(Func<bool> condition, string name)
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new TimeoutException(name);
                }
                await Task.Yield();
            }
        }

        /// <summary>
        /// 실제 조건을 만족한 검사만 통과 목록에 남긴다.
        /// </summary>
        private void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
            _result.checks.Add(message);
        }

        /// <summary>
        /// 렌더가 없는 Editor 검사에서는 캡처를 생략하고 플레이어에서 수행한다.
        /// </summary>
        private Task CaptureAsync(string name)
        {
            if (Application.isEditor && Application.isBatchMode)
            {
                return Task.CompletedTask;
            }
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
            StartCoroutine(CaptureFrame(name, completion));
            return completion.Task;
        }

        /// <summary>
        /// 실제 프레임 렌더 후 화면을 편집하지 않고 PNG 원본으로 저장한다.
        /// </summary>
        private IEnumerator CaptureFrame(string name, TaskCompletionSource<bool> completion)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                Require(texture != null && texture.width == 1280 && texture.height == 720, "Actual rendered frame: " + name);
                File.WriteAllBytes(Path.Combine(_output, name), texture.EncodeToPNG());
                completion.TrySetResult(true);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
            finally
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        /// <summary>
        /// 기능 검사가 끝나도 예외·오류 로그가 있으면 전체 성공으로 처리하지 않는다.
        /// </summary>
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _result.errors.Add(message);
            }
        }
    }
}
