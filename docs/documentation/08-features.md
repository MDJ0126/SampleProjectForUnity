# 구현 기능과 소스

| 기능 | 주요 소스 |
| --- | --- |
| 빙의·입력 | PlayerController, RobotKylePlayerController |
| 이동·애니메이션 | Movement, CharacterAnimationController |
| 카메라·조준 | PlayerCameraController, SpringArm, AimTarget |
| 스킬·버프·능력치 | SkillManager, BuffManager, StatusInfo |
| HUD | HUDManager, FollowHUD, FollowPawnInfo |
| 씬 전환·UI 관리 | SceneControlManager, UIManager, SceneContent, UIView |
| 공통 전투·Behavior AI | Pawn, MeleeWeapon, Gun, AIController, CombatAction |
| 공통 도구 | ObjectPool, Singleton, YieldInstructionCache |

소스는 [Assets/Scripts](../../Assets/Scripts)에 있습니다.

씬·UI 카탈로그와 실행 예제는 [씬 전환과 UI 관리](26-developed-scene-ui.md)에서 확인합니다.

## 현재 주의점

- 스킬 등록·UpdateTick은 Execute를 자동 호출하지 않음. UseSkill로 실행.
- 체력은 SetHp/AddHp에서 범위를 제한하고 변경 이벤트를 발행합니다. HealSkill도 같은 API를 사용합니다.
- HUD 부착 시 SetPawn을 호출하여 이름·체력 데이터를 연결함.
- 버프는 경과 시간으로 종료하며 역순 갱신으로 종료 중 제거를 허용합니다. HealBuff는 초당10, 기본5초입니다.
- SkillObject와 BuffSkill은 기존 확장 뼈대입니다. 소켓은 별도 스크립트 없이 Transform으로 설정합니다.

전투와 Behavior·장비 예제는 [공통 전투](27-developed-combat.md), 실제 검사 근거는 docs/verification/combat에서 확인합니다.
