# 환경과 라이선스

| 항목 | 선언 버전 |
| --- | --- |
| Unity | 6000.0.83f1 |
| URP | 17.0.4 |
| Cinemachine | 3.1.7 |
| Input System | 1.19.0 |
| Animation Rigging | 1.4.1 |
| Addressables | 2.9.1 |
| AI Navigation | 2.0.14 |
| Behavior | 1.0.16 |
| Timeline | 1.8.13 |
| Visual Scripting | 1.9.12 |
| MCP for Unity | v10.0.0 |

근거: [ProjectVersion](../../ProjectSettings/ProjectVersion.txt), [manifest](../../Packages/manifest.json), [lock](../../Packages/packages-lock.json). 현재 작업에서 Unity6000.0.83f1 및 Behavior1.0.16의 실제 컴파일·Play와 패키지 소스를 확인했습니다.

- 자체 씬: Splash, Playground, CharacterStage. 플레이어 시작은 Splash입니다.
- 입력: RobotKyle 액션 맵. 카메라는 기존 입력을 유지하며 F8은 Input System을 사용합니다.
- Ground 레이어: 3. 코드도 같은 이름·값 사용.

## 외부 에셋

README에서 옮긴 에셋 목록입니다. 버전은 당시 기록이며 현재 설치 버전은 미확인입니다.

| 에셋 | 내용 |
| --- | --- |
| Robot Kyle | 로봇 캐릭터 모델과 관련 리소스 |
| Starter Assets | 1인칭·3인칭 컨트롤러와 샘플 리소스 |
| Kevin Iglesias — Human Character Dummy | 남녀 더미 캐릭터 모델 |
| Low Poly Weapons VOL.1 | 현대 화기·장비 모델과 프리팹 |
| PurePoly — Free Fantasy RPG Weapons | 판타지 무기·장비 모델과 프리팹 |

### Kevin Iglesias — Human Animations (FREE)

| 애니메이션 | 기록 버전 |
| --- | --- |
| Human Basic Motions | 2.4 |
| Human Archer Animations | 2.0 |
| Human Melee Animations | 2.0 |
| Human Soldier Animations | 2.0 |
| Human Spellcasting Animations | 2.0 |
| Human Throwing Animations | 2.0 |
| Human Crafting Animations | 2.2 |
| Human Dance Animations | 2.1 |

## 라이선스

### 직접 작성한 코드

직접 작성한 코드는 저장소의 [MIT License 원문](../../LICENSE)을 따릅니다. 저작권 표기는 Copyright (c) 2026 Moondongjun입니다.

- 사용·복사·수정·병합·배포·재라이선스·판매를 허용합니다.
- 복사본이나 주요 부분에 저작권 표시와 허가 문구를 포함해야 합니다.
- 코드는 있는 그대로 제공하며 보증과 책임 제한은 원문을 따릅니다.

### Unity 패키지와 외부 에셋

Unity 패키지와 서드파티 에셋에는 각 제작자가 정한 별도 라이선스가 적용됩니다. 저장소의 MIT License가 외부 에셋 전체에 적용되는 것은 아닙니다.

개별 사용·수정·재배포 조건은 해당 패키지나 에셋의 라이선스 원문에서 확인합니다. 무료 에셋도 동일하게 확인하며, 이번 문서 작업에서는 개별 에셋의 라이선스 조건을 검증하지 않았습니다.

### 다이어그램 라이브러리

문서에 포함된 Mermaid 11.12.0은 별도의 [MIT 라이선스 원문](vendor/mermaid.LICENSE)을 제공합니다. 해당 라이선스 파일을 라이브러리와 함께 보관합니다.
