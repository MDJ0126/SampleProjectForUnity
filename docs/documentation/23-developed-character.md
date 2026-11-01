# 캐릭터 · 플레이어 제어

- **캐릭터 · 빙의**: 컴포넌트 구성, 기본 캐릭터 빙의 및 입력 연결\
  [Character.cs](../../Assets/Scripts/InGame/Object/Pawn/Character/Character.cs), [PlayerController.cs](../../Assets/Scripts/InGame/Object/PlayerController.cs)
- **이동**: 카메라 기준 이동, 달리기·점프·회전, 지면·경사 판정\
  [Movement.cs](../../Assets/Scripts/InGame/Object/Pawn/Movement.cs)
- **애니메이션 · Foot IK**: 이동 상태 반영, 발 위치·몸체 높이 보정\
  [CharacterAnimationController.cs](../../Assets/Scripts/InGame/Object/Pawn/Character/CharacterAnimationController.cs), [CharacterAnimationController.FootIK.cs](../../Assets/Scripts/InGame/Object/Pawn/Character/CharacterAnimationController.FootIK.cs)
- **조준 · 카메라**: 화면 중앙 조준, 궤도 회전, 장애물 대응\
  [AimTarget.cs](../../Assets/Scripts/InGame/Object/Pawn/Character/AimTarget.cs), [PlayerCameraController.cs](../../Assets/Scripts/Common/PlayerCameraController.cs)
- **스킬 · 버프 · 능력치**: 스킬·지속 효과 관리, 캐릭터 상태 데이터\
  [SkillManager.cs](../../Assets/Scripts/InGame/Skill/SkillManager.cs), [BuffManager.cs](../../Assets/Scripts/InGame/Buff/BuffManager.cs), [StatusInfo.cs](../../Assets/Scripts/InGame/Status/StatusInfo.cs)

공통 공격·장비·사망과 Behavior 교전은 [공통 전투](27-developed-combat.md), 실행 범위와 결과는 docs/verification/combat에서 확인합니다.
