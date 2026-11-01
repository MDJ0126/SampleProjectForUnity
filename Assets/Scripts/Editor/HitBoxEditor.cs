using Game;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HitBox의 역할, 공격 스윕 및 디버그 설정을 기능별 카드와 설명으로 표시한다.
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(HitBox), true)]
public class HitBoxEditor : Editor
{
    private SerializedProperty _role;
    private SerializedProperty _sweepEnabled;
    private SerializedProperty _debugEnabled;
    private GUIStyle _titleStyle;
    private GUIStyle _descriptionStyle;

    private void OnEnable()
    {
        _role = serializedObject.FindProperty("Role");
        _sweepEnabled = serializedObject.FindProperty("isAttackSweepEnabled");
        _debugEnabled = serializedObject.FindProperty("isHitDebugEnabled");
    }

    /// <summary>
    /// 직렬화 프로퍼티를 통해 다중 선택, Undo 및 프리팹 오버라이드를 유지한다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InitializeStyles();
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }
        EditorGUILayout.Space(6f);
        DrawRole();
        DrawSweep();
        DrawDebug();
        DrawPropertiesExcluding(serializedObject, "m_Script", "Role", "isAttackSweepEnabled", "sweepStepDistance", "maxSweepSteps", "sweepDebugDuration", "sweepDebugColor", "sweepDebugHitColor", "isHitDebugEnabled", "hitDebugColor", "hitDebugDuration", "hitDebugSize", "isHitDebugDepthTest");
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 현재 에디터 테마의 기본 스타일을 바탕으로 제목과 줄바꿈 설명 스타일을 생성한다.
    /// </summary>
    private void InitializeStyles()
    {
        if (_titleStyle != null)
        {
            return;
        }
        _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        _descriptionStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { padding = new RectOffset(0, 0, 2, 6) };
    }

    /// <summary>
    /// 역할과 실행 중 확인할 수 있는 소유자를 표시한다.
    /// </summary>
    private void DrawRole()
    {
        BeginCard("HIT BOX", "공격과 피격을 연결하는 충돌 판정", new Color(0.3f, 0.65f, 0.95f));
        EditorGUILayout.PropertyField(_role, new GUIContent("판정 역할"));
        string description = _role.hasMultipleDifferentValues ? "여러 역할을 함께 선택했습니다. 각 HitBox의 역할에 따라 동작합니다."
            : _role.enumValueIndex == (int)HitBox.eHitBoxRole.Attack ? "Attack · 공격을 전달하는 판정입니다. 상대 Hurt HitBox와의 충돌을 처리합니다."
            : "Hurt · 공격을 받는 판정입니다. 상대 Attack HitBox와의 충돌을 처리합니다.";
        EditorGUILayout.LabelField(description, _descriptionStyle);
        if (Application.isPlaying && targets.Length == 1)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("소유자", ((HitBox)target).Owner, typeof(Pawn), true);
            }
        }
        EndCard();
    }

    /// <summary>
    /// 공격 스윕의 원리와 검사 비용을 설명하고 Attack 역할에만 설정을 허용한다.
    /// </summary>
    private void DrawSweep()
    {
        BeginCard("ATTACK SWEEP", "프레임 사이에서 놓친 공격을 보완", new Color(1f, 0.65f, 0.25f));
        EditorGUILayout.HelpBox("이전 프레임과 현재 프레임 사이의 위치·회전을 나눠 검사합니다. 빠르게 휘두르는 공격을 보완하며 Attack 역할에서만 동작합니다.\n검사 간격이 작거나 최대 분할 수가 클수록 추가 물리 검사 비용이 증가합니다.", MessageType.Info);
        bool canSweep = _role.hasMultipleDifferentValues || _role.enumValueIndex == (int)HitBox.eHitBoxRole.Attack;
        using (new EditorGUI.DisabledScope(!canSweep))
        {
            DrawToggle(_sweepEnabled, "프레임 사이 검사 사용");
            using (new EditorGUI.DisabledScope(!_sweepEnabled.hasMultipleDifferentValues && !_sweepEnabled.boolValue))
            {
                DrawField("sweepStepDistance", "검사 간격", "월드 단위입니다. 작을수록 이동·회전을 촘촘하게 검사합니다.");
                DrawField("maxSweepSteps", "최대 분할 수", "한 프레임의 분할 한도입니다. 한도를 넘는 이동은 검사 간격이 커집니다.");
            }
        }
        if (!canSweep)
        {
            EditorGUILayout.LabelField("Hurt 역할에서는 스윕 검사를 수행하지 않습니다.", _descriptionStyle);
        }
        EndCard();
    }

    /// <summary>
    /// 충돌 지점과 후보 검색 궤적의 차이 및 전역 디버그의 OR 조건을 설명한다.
    /// </summary>
    private void DrawDebug()
    {
        BeginCard("DEBUG VISUALS", "게임뷰에서 충돌 지점과 검사 궤적 확인", new Color(0.35f, 0.8f, 0.6f));
        EditorGUILayout.HelpBox("충돌 지점은 정육면체로 표시합니다. 스윕이 꺼져 있거나 Hurt 역할이면 현재 HitBox를 콜라이더 종류에 맞는 캡슐·구·박스로 실시간 표시하며 추가 충돌 검사는 하지 않습니다. Attack Sweep이 켜지면 검사 궤적을 박스로 남깁니다. 궤적 박스는 후보 검색 범위이며 실제 타격은 원래 콜라이더의 겹침으로 판단합니다.\n개별 디버그 또는 GameConfig.IsCollisionDebugEnabled가 켜지면 표시합니다. 빌드에서도 표시되며 유지 시간 0은 한 프레임입니다.", MessageType.Info);
        EditorGUILayout.LabelField(GameConfig.IsCollisionDebugEnabled ? "● 전역 디버그 켜짐 · 개별 체크와 관계없이 표시" : "○ 전역 디버그 꺼짐 · 개별 체크로 표시", EditorStyles.miniBoldLabel);
        DrawToggle(_debugEnabled, "HitBox 디버그 표시");
        bool canDebug = GameConfig.IsCollisionDebugEnabled || _debugEnabled.hasMultipleDifferentValues || _debugEnabled.boolValue;
        using (new EditorGUI.DisabledScope(!canDebug))
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("충돌 지점", EditorStyles.boldLabel);
            DrawField("hitDebugColor", "표식 색상");
            DrawField("hitDebugDuration", "유지 시간 (초)");
            DrawField("hitDebugSize", "정육면체 크기", "충돌 지점 정육면체 한 변의 길이입니다. 현재 HitBox 영역 크기는 콜라이더에서 가져옵니다.");
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("스윕 검사 궤적", EditorStyles.boldLabel);
            bool canSweep = (_role.hasMultipleDifferentValues || _role.enumValueIndex == (int)HitBox.eHitBoxRole.Attack) && (_sweepEnabled.hasMultipleDifferentValues || _sweepEnabled.boolValue);
            using (new EditorGUI.DisabledScope(!canSweep))
            {
                DrawField("sweepDebugDuration", "궤적 유지 시간 (초)");
                DrawField("sweepDebugColor", "미감지 색상");
                DrawField("sweepDebugHitColor", "감지 색상");
            }
            EditorGUILayout.Space(5f);
            DrawField("isHitDebugDepthTest", "다른 물체에 가려지기", "켜면 다른 물체 뒤의 디버그 표시를 숨깁니다. 충돌 지점과 궤적에 함께 적용합니다.");
        }
        EndCard();
    }

    /// <summary>
    /// 강조색과 제목을 사용하여 기능별 카드의 시작을 표시한다.
    /// </summary>
    private void BeginCard(string title, string description, Color accent)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Rect stripe = GUILayoutUtility.GetRect(0f, 3f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(stripe, accent);
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(title, _titleStyle);
        EditorGUILayout.LabelField(description, _descriptionStyle);
    }

    private static void EndCard()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6f);
    }

    private void DrawField(string name, string label, string tooltip = null)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label, tooltip), true);
    }

    /// <summary>
    /// 기본 Header 장식의 중복을 피하면서 다중 값과 프리팹 오버라이드 표시를 유지한다.
    /// </summary>
    private static void DrawToggle(SerializedProperty property, string label)
    {
        Rect rect = EditorGUILayout.GetControlRect();
        GUIContent content = EditorGUI.BeginProperty(rect, new GUIContent(label), property);
        bool previousMixedValue = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        bool value = EditorGUI.Toggle(rect, content, property.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.boolValue = value;
        }
        EditorGUI.showMixedValue = previousMixedValue;
        EditorGUI.EndProperty();
    }
}
/// <summary>
/// 저장하지 않는 디버그 객체를 에디터의 Play 종료와 재로드에서 정리한다.
/// </summary>
[InitializeOnLoad]
internal static class PhysicsQueryDebugLifecycle
{
    static PhysicsQueryDebugLifecycle()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        EditorApplication.delayCall += CleanupAfterReload;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            Cleanup();
        }
    }

    private static void CleanupAfterReload()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Cleanup();
        }
    }

    /// <summary>
    /// 재로드 전에 생성한 임시 렌더러와 메시를 함께 제거한다.
    /// </summary>
    private static void Cleanup()
    {
        foreach (PhysicsQueryDebugRenderer renderer in Resources.FindObjectsOfTypeAll<PhysicsQueryDebugRenderer>())
        {
            if (renderer == null || EditorUtility.IsPersistent(renderer))
            {
                continue;
            }
            renderer.DisposeResources(true);
            Object.DestroyImmediate(renderer.gameObject);
        }
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }
}
