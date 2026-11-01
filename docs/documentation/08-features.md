# 구현 기능과 소스

| 기능 | 주요 소스 |
| --- | --- |
| 빙의·입력 | PlayerController, RobotKylePlayerController |
| 이동·애니메이션 | Movement, CharacterAnimationController |
| 카메라·조준 | PlayerCameraController, SpringArm, AimTarget |
| 스킬·버프·능력치 | SkillManager, BuffManager, StatusInfo |
| HUD | HUDManager, FollowHUD, FollowPawnInfo |
| 씬 전환·UI 관리 | SceneControlManager, UIManager, SceneContent, UIView |
| 공통 도구 | ObjectPool, Singleton, YieldInstructionCache |

소스는 [Assets/Scripts](../../Assets/Scripts)에 있습니다.

씬·UI 카탈로그와 실행 예제는 [씬 전환과 UI 관리](26-developed-scene-ui.md)에서 확인합니다.

## 현재 주의점

- 스킬 등록·UpdateTick은 Execute를 자동 호출하지 않음. UseSkill로 실행.
- SetHp는 체력을 대입하고 이벤트를 발행함. HealSkill의 직접 hp 변경은 이벤트를 발행하지 않음.
- HUD 부착 시 SetPawn을 호출하여 이름·체력 데이터를 연결함.
- 버프 종료 조건·순회 중 제거는 실행 검증 필요.
- Socket, SkillObject, BuffSkill은 확장 뼈대.

위 내용은 소스로 확인했습니다. 런타임 결과는 미확인입니다.
