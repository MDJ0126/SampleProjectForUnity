# 캐릭터 다이어그램

## 캐릭터 내부 구성

`Pawn`을 상속하는 `Character`를 기준으로 묶었습니다. 아래 박스의 배치는 실행 순서가 아니라 기능별 구성 목록입니다.

```mermaid
---
config:
  theme: base
  themeVariables:
    background: "#b8c7d9"
    primaryColor: "#edf2f8"
    primaryTextColor: "#1e293b"
    primaryBorderColor: "#94a3b8"
    lineColor: "#64748b"
    secondaryColor: "#d6e0ed"
    tertiaryColor: "#d6e0ed"
    clusterBkg: "#d6e0ed"
    clusterBorder: "#8da2ba"
    titleColor: "#334155"
    edgeLabelBackground: "#d6e0ed"
  themeCSS: |
    a, a:link, a:visited, a:hover, a:active, a:focus, a *,
    .node a, .node a:link, .node a:visited, .node a:hover,
    .node .nodeLabel, .node .nodeLabel *, .node text {
      color: #1e293b !important;
      fill: #1e293b !important;
      text-decoration: none !important;
      text-decoration-line: none !important;
    }
---
flowchart TB
    subgraph Character["Character / Pawn — 내부 구성"]
        direction TB
        Core["Pawn → Character<br/>컴포넌트 참조 · 빙의 / 빙의 해제"]
        Core --- Motion
        Core --- Gameplay
        Core --- Targets
        Core -->|공통 공격·교체·수납 요청| Combat["Weapon / MeleeWeapon / Gun<br/>무기 목록 · 소켓 · 피격 판정 · 탄약"]
        Combat -->|팀·방어력·사망 검사| Status

        subgraph Motion["이동 · 애니메이션"]
            direction TB
            Movement["Movement Component<br/>이동 · 달리기 · 점프 · 회전"]
            Animation["Animation Controller<br/>이동 상태 조회 · 애니메이션 갱신"]
            Movement ~~~ Animation
        end

        subgraph Gameplay["스킬 · 버프 · 능력치"]
            direction TB
            Skills["Skill Manager / Skill<br/>스킬 보유 · 갱신"]
            Buffs["Buff Manager / Buff<br/>지속 효과 · 수명 관리"]
            Status["StatusInfo / Status<br/>현재 체력 · 기본 능력치"]
            Skills ~~~ Buffs ~~~ Status
        end

        subgraph Targets["조준 · 카메라 · HUD 기준점"]
            direction TB
            Aim["Aim Target / Aim Rig<br/>조준 위치 · Rig 가중치"]
            SpringArm["Spring Arm<br/>카메라 기준 위치 · 회전"]
            Anchor["Widget Anchor<br/>이름 · 말풍선 · 체력바 기준점"]
            Aim ~~~ SpringArm ~~~ Anchor
        end
    end

    classDef default fill:#edf2f8,stroke:#8da2ba,color:#1e293b
    classDef focus fill:#b9d3ef,stroke:#527fae,color:#1e293b,stroke-width:2px
    class Core focus

    style Character fill:#b8c7d9,stroke:#8da2ba,color:#1e293b
    style Motion fill:#d6e0ed,stroke:#9bafc5,color:#1e293b
    style Gameplay fill:#d6e0ed,stroke:#9bafc5,color:#1e293b
    style Targets fill:#d6e0ed,stroke:#9bafc5,color:#1e293b

    click Core href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/InGame/Object/Pawn/Pawn.cs" "Pawn.cs 열기" _blank
    click Movement href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/InGame/Object/Pawn/Movement.cs" "Movement.cs 열기" _blank
    click Animation href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/InGame/Object/Pawn/Character/CharacterAnimationController.cs" "CharacterAnimationController.cs 열기" _blank
    click Skills href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/InGame/Skill/SkillManager.cs" "SkillManager.cs 열기" _blank
    click Buffs href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/InGame/Buff/BuffManager.cs" "BuffManager.cs 열기" _blank
    click Status href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/InGame/Status/StatusInfo.cs" "StatusInfo.cs 열기" _blank
    click Aim href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/InGame/Object/Pawn/Character/AimTarget.cs" "AimTarget.cs 열기" _blank
    click SpringArm href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/Common/SpringArm.cs" "SpringArm.cs 열기" _blank
    click Anchor href "https://github.com/MDJ0126/SampleProjectForUnity/blob/main/Assets/Scripts/UI/HUD/WidgetAnchor.cs" "WidgetAnchor.cs 열기" _blank
```


현재 HUD 앵커는 PawnInfoAnchor와 balloonAnchor입니다. 이름·체력 데이터는 별도 SetPawn 연결이 필요합니다. 실제 실행은 미확인입니다.

[인게임 전체 연결](07-diagrams.md)을 참고합니다.
