using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game
{
    /// <summary>
    /// 명시적인 검사 실행에서 실제 전투·Behavior·씬·UI를 검증한다.
    /// </summary>
    public class CombatVerification : MonoBehaviour
    {
        [Serializable]
        private class Result
        {
            public string startedAt;
            public string completedAt;
            public bool passed;
            public List<string> checks = new List<string>();
            public List<string> trace = new List<string>();
            public List<string> errors = new List<string>();
        }
        private readonly Result _result = new Result();
        private string _output;
        private SceneControlManager _control;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-combatVerify");
            if (index >= 0 && index + 1 < args.Length)
            {
                GameObject runner = new GameObject("Combat Verification");
                DontDestroyOnLoad(runner);
                runner.AddComponent<CombatVerification>()._output = Path.GetFullPath(args[index + 1]);
            }
        }

        private async void Start()
        {
            _result.startedAt = DateTimeOffset.Now.ToString("O");
            Directory.CreateDirectory(_output);
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.logMessageReceived += OnLog;
            try
            {
                await Run();
                _result.passed = _result.errors.Count == 0;
            }
            catch (Exception exception)
            {
                _result.errors.Add(exception.ToString());
                Debug.LogError("[Combat QA] " + exception);
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
                _result.completedAt = DateTimeOffset.Now.ToString("O");
                File.WriteAllText(Path.Combine(_output, "results.json"), JsonUtility.ToJson(_result, true));
                Debug.Log("[Combat QA] " + (_result.passed ? "PASS" : "FAIL") + " / " + _result.checks.Count);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(_result.passed ? 0 : 1);
#endif
            }
        }

        /// <summary>
        /// 원본 씬의 배치 오브젝트와 실제 물리·그래프 결과를 순서대로 대조한다.
        /// </summary>
        private async Task Run()
        {
            await Wait(() => SceneControlManager.Instance != null && SceneControlManager.Instance.CurrentSceneId == "Playground" && !SceneControlManager.Instance.IsTransitioning, "Splash to Playground");
            _control = SceneControlManager.Instance;
            await Wait(() => GameMode.Instance != null && GameMode.Instance.playerController.IsPossessed, "player possession");
            var mode = (SampleSceneGameMode)GameMode.Instance;
            Pawn player = mode.defaultPawn;
            AIController ai = mode.opponent;
            Pawn enemy = ai.controlledPawn;
            SceneFlowContent content = FindFirstObjectByType<SceneFlowContent>();
            await Delay(0.1f);
            Require(content.IsMenuOpen && !player.IsCombatEnabled && !enemy.IsCombatEnabled, "Menu pauses both fighters");
            Require(player.Movement.IsGrounded, "Paused spawn still computes its grounded animation state");
            Require(player.equipmentList.Count == 2 && enemy.equipmentList.Count == 2, "Two authored weapons per fighter");
            Require(ai.GetComponent<BehaviorGraphAgent>().Graph != null && ai.GetComponent<NavMeshAgent>().isOnNavMesh, "Native Behavior runtime graph and baked NavMesh loaded");
            Require(PlayerController.Instance == mode.playerController && (PawnController)ai is not PlayerController, "AI does not replace player camera/input ownership");
            Require(!player.TryAttack(enemy.AimPoint), "Menu blocks attacks");
            await Capture("01-playground-menu.png");
            content.ToggleMenu();
            ai.enabled = false;
            ai.GetComponent<BehaviorGraphAgent>().enabled = false;
            Require(player.SelectWeapon(0) && player.EquippedWeapon is MeleeWeapon, "Select owned sword");
            player.SetWeaponDrawn(false);
            Require(!player.TryAttack(enemy.AimPoint) && player.EquippedWeapon.transform.parent == player.holsterSocket, "Sheath moves sword to authored back socket and blocks attack");
            player.SetWeaponDrawn(true);
            Require(player.EquippedWeapon.transform.parent == player.weaponSocket, "Draw restores hand socket");
            player.SetWeaponDrawn(false, true);
            Require(player.IsChangingWeapon && !player.TryAttack(enemy.AimPoint), "Sheath animation locks attack until handoff completes");
            await Wait(() => !player.IsChangingWeapon, "animated sheath");
            Require(!player.IsWeaponDrawn && player.EquippedWeapon.transform.parent == player.holsterSocket, "Animated sheath commits back socket");
            player.SetWeaponDrawn(true, true);
            await Wait(() => !player.IsChangingWeapon, "animated draw");
            Require(player.IsWeaponDrawn && player.EquippedWeapon.transform.parent == player.weaponSocket, "Animated draw commits hand socket");
            int inventory = player.equipmentList.Count;
            for (int i = 0; i < 30; i++) { player.SelectNextWeapon(); }
            Require(player.equipmentList.Count == inventory, "Thirty swaps preserve inventory without duplicates");
            Require(!player.SelectWeapon(-1) && !player.SelectWeapon(99) && !player.Equip(enemy.EquippedWeapon), "Invalid selection and foreign ownership rejected");
            Require(player.SelectWeapon(1) && player.EquippedWeapon is Gun, "Select owned rifle");
            var gun = (Gun)player.EquippedWeapon;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState().WithButton(MouseButton.Left));
            await Delay(0.4f);
            Require(gun.ShotsFired >= 1, "Actual PlayerInput mouse press fires the rifle");
            InputSystem.QueueStateEvent(Mouse.current, new MouseState());
            await Delay(0.15f);
            int released = gun.ShotsFired;
            await Delay(0.65f);
            Require(gun.ShotsFired == released, "Actual mouse release stops automatic fire");
            player.CancelAttack();
            gun.ResetWeapon();
            enemy.ResetCombat();
            Vector3 originalPlayer = player.transform.position;
            Vector3 originalEnemy = enemy.transform.position;
            player.Movement.enabled = false;
            enemy.Movement.enabled = false;
            player.GetComponent<Rigidbody>().isKinematic = true;
            enemy.GetComponent<Rigidbody>().isKinematic = true;
            player.transform.position = new Vector3(0, 0.1f, 0);
            enemy.transform.position = new Vector3(0, 0.1f, 4);
            player.transform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            await Delay(0.1f);
            int hp = enemy.StatusInfo.hp;
            Require(player.TryAttack(enemy.AimPoint), "Rifle attack accepted");
            Require(enemy.StatusInfo.hp == hp - gun.Damage && gun.Ammo == gun.magazineSize - 1 && gun.LastHitPawn == enemy, "Actual muzzle ray damages enemy and spends exactly one round");
            Require(!player.TryAttack(enemy.AimPoint), "Cooldown rejects duplicate attack");
            await Delay(0.4f);
            Require(gun.Reload() && !gun.Reload(), "Reload accepts once");
            await Delay(gun.reloadDuration + 0.1f);
            Require(gun.Ammo == gun.magazineSize && gun.Reserve == gun.reserveAmmo - 1, "Timed reload conserves ammunition");
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "QA occlusion wall";
            wall.transform.position = new Vector3(0, 1, 2);
            wall.transform.localScale = new Vector3(4, 3, 0.3f);
            Physics.SyncTransforms();
            hp = enemy.StatusInfo.hp;
            Require(player.TryAttack(enemy.AimPoint) && enemy.StatusInfo.hp == hp && gun.LastHitPawn == null, "Solid obstacle blocks real rifle damage");
            Destroy(wall);
            await Delay(0.4f);
            enemy.teamId = player.teamId;
            Require(player.TryAttack(enemy.AimPoint) && enemy.StatusInfo.hp == hp, "Friendly body blocks shot without friendly damage");
            enemy.teamId = 1;
            await Delay(0.4f);
            Require(gun.Reload(), "Reload after blocked shots");
            player.SelectWeapon(0);
            Require(!gun.IsReloading, "Swapping cancels reload");
            player.SetWeaponDrawn(false);
            await Capture("02-sword-sheathed.png");
            player.SetWeaponDrawn(true);
            player.StatusInfo.SetHp(int.MaxValue);
            Require(player.StatusInfo.hp == player.StatusInfo.MaxHp, "Health clamps overheal");
            player.StatusInfo.Damaged(-20);
            Require(player.StatusInfo.hp == player.StatusInfo.MaxHp, "Negative damage does not heal");
            player.StatusInfo.SetHp(-1);
            Require(player.IsDead && !mode.playerController.IsPossessed && !player.TryAttack(enemy.AimPoint), "Death releases possession and blocks attack");
            Require(!player.Damaged(enemy, new DamageInfo { damage = 10 }), "Dead target rejects repeat damage");
            await Delay(1.2f);
            Require(player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Dead"), "Authored death animation reaches Dead state");
            await Capture("03-defeat.png");
            content.RestartDuel();
            Require(!player.IsDead && player.StatusInfo.hp == player.StatusInfo.MaxHp && mode.playerController.IsPossessed, "Restart restores health and player possession");
            Require(player.Movement.enabled && !player.GetComponent<Rigidbody>().isKinematic && enemy.GetComponentInChildren<CapsuleCollider>().enabled, "Restart restores physics and movement");
            player.transform.position = originalPlayer;
            enemy.transform.position = originalEnemy;
            enemy.SelectWeapon(0);
            ai.enabled = true;
            enemy.Movement.isMoveEnable = false;
            ai.GetComponent<NavMeshAgent>().Warp(enemy.transform.position);
            Require(ai.Approach(null) == Node.Status.Failure, "Missing AI target fails safely");
            Require(ai.Approach(player) == Node.Status.Running, "Approach requests actual baked path");
            await Delay(3.1f);
            Require(ai.Approach(player) == Node.Status.Failure && !ai.HasPath, "Blocked movement abandons path after bounded timeout");
            enemy.Movement.isMoveEnable = true;
            ai.GetComponent<BehaviorGraphAgent>().enabled = true;
            ai.GetComponent<NavMeshAgent>().Warp(enemy.transform.position);
            int before = player.StatusInfo.hp;
            float nextTrace = 0f;
            await Wait(() =>
            {
                if (Time.time > nextTrace)
                {
                    nextTrace = Time.time + 1f;
                    NavMeshAgent nav = ai.GetComponent<NavMeshAgent>();
                    MeleeWeapon melee = enemy.EquippedWeapon as MeleeWeapon;
                    _result.trace.Add("t=" + Time.time + " enemy=" + enemy.transform.position + " target=" + player.transform.position + " poss=" + ai.IsPossessed + " combat=" + enemy.IsCombatEnabled + " attack=" + enemy.IsAttacking + " hit=" + (melee != null && melee.hitBox.enabled) + " input=" + enemy.Movement.MoveInput + " nav=" + nav.isOnNavMesh + "/" + nav.hasPath + "/" + nav.pathStatus + " find=" + (ai.FindTarget() == player) + " damage=" + enemy.CanDamage(player) + " hp=" + player.StatusInfo.hp + "/" + enemy.StatusInfo.hp + " teams=" + player.teamId + "/" + enemy.teamId + " active=" + player.isActiveAndEnabled + " pc=" + player.IsCombatEnabled + " registry=" + Pawn.ActivePawns.Count);
                }
                return player.StatusInfo.hp < before;
            }, "Behavior find/approach/melee damage", 20f);
            Require(ai.PossessedPawn == enemy && player.StatusInfo.hp < before, "Actual Behavior graph navigates and inflicts melee damage");
            await Capture("04-behavior-duel.png");
            await Wait(() => player.IsDead, "Behavior duel reaches death", 30f);
            Require(content.IsMenuOpen && !enemy.IsCombatEnabled, "Actual AI victory pauses duel and opens restart menu");
            content.RestartDuel();
            enemy.SelectWeapon(1);
            before = player.StatusInfo.hp;
            await Wait(() => player.StatusInfo.hp < before, "Behavior rifle damage", 8f);
            Require(((Gun)enemy.EquippedWeapon).ShotsFired > 0, "Actual Behavior graph also fires the shared rifle");
            GameObject other = Instantiate(enemy.gameObject, enemy.transform.position + Vector3.right * 3f, Quaternion.identity);
            other.GetComponent<Pawn>().teamId = -1;
            await Delay(0.1f);
            BehaviorGraphAgent firstGraph = ai.GetComponent<BehaviorGraphAgent>();
            BehaviorGraphAgent secondGraph = other.GetComponent<BehaviorGraphAgent>();
            bool firstFound = firstGraph.GetVariable("Target", out BlackboardVariable firstTarget);
            bool secondFound = secondGraph.GetVariable("Target", out BlackboardVariable secondTarget);
            Require(firstGraph.Graph != secondGraph.Graph && firstFound && secondFound && !ReferenceEquals(firstTarget, secondTarget), "Two agents own isolated graph instances and Target blackboards");
            Require(PlayerController.Instance == mode.playerController, "Second AI leaves player singleton unchanged");
            Destroy(other);
            await Delay(0.1f);
            content.ToggleMenu();
            before = player.StatusInfo.hp;
            await Delay(1f);
            Require(player.StatusInfo.hp == before, "Menu stops active AI combat");
            await _control.ChangeSceneAsync("CharacterStage");
            content = FindFirstObjectByType<SceneFlowContent>();
            var view = (SceneFlowView)content.screenView;
            Button rifle = Array.Find(view.GetComponentsInChildren<Button>(true), button => button.name == "RifleButton");
            Require(rifle != null && rifle.onClick.GetPersistentEventCount() == 1, "Rifle selection is an authored Inspector event");
            rifle.onClick.Invoke();
            Pawn preview = content.previewCharacter.GetComponent<Pawn>();
            Require(preview.EquippedWeapon is Gun && view.loadout.SelectedIndex == 1, "Stage button equips actual preview and shared selection");
            EquipmentLoadout duplicate = Instantiate(view.loadout);
            duplicate.Select(0);
            Require(view.loadout.SelectedIndex == 0, "Duplicated bundle data shares selection by explicit session id");
            duplicate.selectionId = "QA independent";
            duplicate.Select(0);
            view.SelectWeapon(1);
            Require(duplicate.SelectedIndex == 0 && view.loadout.SelectedIndex == 1, "Different loadout session ids remain independent");
            Destroy(duplicate);
            await Delay(0.3f);
            await Capture("05-stage-rifle.png");
            await _control.ChangeSceneAsync("Playground");
            await Wait(() => GameMode.Instance.playerController.IsPossessed, "return possession");
            mode = (SampleSceneGameMode)GameMode.Instance;
            Require(mode.defaultPawn.EquippedWeapon is Gun, "Stage rifle selection reaches actual Playground equipment");
            for (int i = 0; i < 3; i++)
            {
                await _control.ChangeSceneAsync("CharacterStage");
                await _control.ChangeSceneAsync("Playground");
            }
            Require(FindObjectsByType<SceneControlManager>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<UIManager>(FindObjectsSortMode.None).Length == 1, "Scene round trips retain single managers");
            Require(Pawn.ActivePawns.Count == FindObjectsByType<Pawn>(FindObjectsSortMode.None).Length, "Pawn registry contains no unloaded actors after scene round trips");
            await Delay(0.5f);
            Pawn returned = GameMode.Instance.defaultPawn;
            float hipsHeight = returned.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Hips).position.y - returned.transform.position.y;
            _result.trace.Add("Returned rifle hips height=" + hipsHeight + " grounded=" + returned.Movement.IsGrounded + " root=" + returned.transform.position);
            Require(returned.Movement.IsGrounded && hipsHeight > 0.4f && hipsHeight < 1.8f, "Returned rifle actor remains grounded in paused menu");
            await Capture("06-return-rifle.png");

        }

        /// <summary>
        /// 실제 프레임을 기다리며 조건 미달은 시간 초과로 남긴다.
        /// </summary>
        private async Task Wait(Func<bool> condition, string name, float seconds = 30f)
        {
            float limit = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > limit)
                {
                    throw new TimeoutException(name);
                }
                await Task.Yield();
            }
        }

        /// <summary>
        /// 게임 시간을 기다려 쿨다운과 장전을 실제 갱신한다.
        /// </summary>
        private Task Delay(float seconds)
        {
            float until = Time.time + seconds;
            return Wait(() => Time.time >= until, "delay");
        }

        /// <summary>
        /// 관찰한 조건만 통과 목록에 추가한다.
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
        /// 렌더링이 있는 실행에서 실제 게임 화면을 저장한다.
        /// </summary>
        private Task Capture(string name)
        {
            if (Application.isBatchMode && Application.isEditor)
            {
                return Task.CompletedTask;
            }
            var completion = new TaskCompletionSource<bool>();
            StartCoroutine(CaptureFrame(name, completion));
            return completion.Task;
        }

        private IEnumerator CaptureFrame(string name, TaskCompletionSource<bool> completion)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(_output, name), texture.EncodeToPNG());
                if (name == "06-return-rifle.png") { CheckMenuPixels(texture); }
                completion.SetResult(true);
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
            finally
            {
                if (texture != null) { Destroy(texture); }
            }
        }

        /// <summary>
        /// 실제 픽셀에서 반복 전환 후 버튼 배경과 제목이 보이는지 확인한다.
        /// </summary>
        private void CheckMenuPixels(Texture2D texture)
        {
            SceneFlowView view = (SceneFlowView)FindFirstObjectByType<SceneFlowContent>().screenView;
            Button resume = Array.Find(view.GetComponentsInChildren<Button>(true), button => button.name == "ResumeButton");
            Text title = Array.Find(view.GetComponentsInChildren<Text>(true), text => text.name == "MenuTitle");
            Color32[] pixels = texture.GetPixels32();
            Require(Count(resume.GetComponent<RectTransform>()) > 1000, "Rendered resume button survives repeated bundle transitions");
            Require(Count(title.rectTransform) > 40, "Rendered menu title survives repeated bundle transitions");
            int Count(RectTransform rect)
            {
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                int count = 0;
                for (int y = Mathf.Max(0, (int)corners[0].y); y < Mathf.Min(texture.height, (int)corners[2].y); y++)
                {
                    for (int x = Mathf.Max(0, (int)corners[0].x); x < Mathf.Min(texture.width, (int)corners[2].x); x++)
                    {
                        Color32 pixel = pixels[y * texture.width + x];
                        if (pixel.g > 150 && pixel.g > pixel.r + 35 && pixel.b > 80) { count++; }
                    }
                }
                return count;
            }
        }
        /// <summary>
        /// 실행 중 예외와 오류도 전체 판정에 포함한다.
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
