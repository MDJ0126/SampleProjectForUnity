# SampleProjectForUnity Instructions

## Document Tree

- [AGENTS](AGENTS.md) — Project instructions
  - [프로젝트 소개](docs/documentation/00-start.md) — Documentation entry point
  - [프로젝트 구조](docs/documentation/01-project.md) — Folders and shared systems
    - [환경과 라이선스](docs/documentation/09-environment.md) — Unity version and third-party assets
  - [작업과 검증](docs/documentation/06-workflow.md) — Planning and verification sequence
    - [Unity 작업 규칙](docs/documentation/04-unity.md) — Editor and asset workflow
    - [예제와 문서 양식](docs/documentation/05-examples.md) — Examples and writing templates
  - [전체 프로세스](docs/documentation/21-process-diagrams.md) — High-level flow · planned
    - [인게임 연결](docs/documentation/07-diagrams.md) — Runtime system relationships
      - [캐릭터 다이어그램](docs/documentation/22-character-diagrams.md) — Character and controller hierarchy
      - [아웃게임 흐름](docs/documentation/20-outgame-diagrams.md) — Outgame flow · planned
  - [구현 기능](docs/documentation/08-features.md) — Implemented features and source references
    - [씬 전환과 UI](docs/documentation/26-developed-scene-ui.md) — Initialization and screen transitions
    - [공통 전투](docs/documentation/27-developed-combat.md) — Weapons, equipment, and AI
      - [캐릭터와 제어](docs/documentation/23-developed-character.md) — Character possession and input
        - [HUD 구현](docs/documentation/24-developed-hud.md) — On-screen information and state
          - [HUD 작성](docs/documentation/16-hud.md) — UI presentation guidance
      - [버프 작성](docs/documentation/14-buff.md) — Status effect authoring
      - [스킬 작성](docs/documentation/15-skill.md) — Skill contracts and flow
    - [유틸리티와 에디터](docs/documentation/25-developed-tools.md) — Development tools and shared modules
      - [에이전트 모델](docs/documentation/28-agent-models.md) — Model requests and game-specific interpretation
      - [WAV 인코딩](docs/documentation/29-wave-encoding.md) — Shared audio-input encoding
  - [네이밍 컨벤션](docs/documentation/02-naming.md) — Naming and code conventions
    - [클래스 작성](docs/documentation/12-class.md) — Responsibilities and state
      - [구조체 작성](docs/documentation/13-struct.md) — Value types and copy semantics
      - [열거형 작성](docs/documentation/18-enum.md) — States and serialized values
    - [함수 작성](docs/documentation/03-functions.md) — Inputs, failure, and state changes
      - [주석과 줄바꿈](docs/documentation/17-comments-format.md) — Comments and control-flow formatting
      - [이벤트 작성](docs/documentation/19-events.md) — Subscription and cleanup
  - [문서 갱신](docs/documentation/10-updating.md) — Source, verification, and publishing
    - [갱신 이력](docs/documentation/11-update-history.md) — Verification baseline and revision history

## Start Here

Follow the user's instructions and preserve existing changes. Use this file to select the source documents relevant to the task.

| When to read | Sources |
| --- | --- |
| Finding how features and documents fit together | [Project introduction](docs/documentation/00-start.md) and [document map](docs/documentation/30-document-map.md) |
| Writing or changing C# | [Naming](docs/documentation/02-naming.md), [functions](docs/documentation/03-functions.md), [classes](docs/documentation/12-class.md), [structs](docs/documentation/13-struct.md), [enums](docs/documentation/18-enum.md), and [comments and formatting](docs/documentation/17-comments-format.md) as applicable |
| Changing events | Read [events](docs/documentation/19-events.md) before editing the feature |
| Implementing a buff, a skill, or HUD behavior | [Buffs](docs/documentation/14-buff.md), [skills](docs/documentation/15-skill.md), or [HUD](docs/documentation/16-hud.md) |
| Planning or checking Unity work | [Unity work rules](docs/documentation/04-unity.md) and [work and verification](docs/documentation/06-workflow.md) |
| Updating the documentation | [Refresh procedure](docs/documentation/10-updating.md) and [last recorded revision](docs/documentation/11-update-history.md) |

## Code and Verification

- Prefer Korean commit messages. Preserve code identifiers, product names, and other proper names in their original form.
- Place Unity lifecycle methods such as `Awake`, `OnEnable`, `Start`, `Update`, `LateUpdate`, `OnDisable`, and `OnDestroy` at the top of the methods section. Place custom methods and callbacks such as `OnEquippedWeaponChanged` below them.
- Keep ordinary-length calls on one line. Write an XML `summary` in three lines. Use Korean declarative comments in the “~한다.” style to describe the contract and reasoning.
- Write `if` and `else` as multiline blocks. Always use braces, including for a one-line body. Write `switch` across multiple lines and use braces for every `case` and `default` body. Put braces on their own lines, and write the label, body, and `break` or `return` on separate lines.
- Verify claims using source, configuration, and execution. Mark unverified items as `미확인`. Remove temporary generation, configuration, and verification scripts and tools when finished, including their Unity `.meta` files and empty temporary folders. Preserve existing files and results that must be kept.

## Documentation

- Compare the changes and their impact since the last recorded SHA and snapshot. If the baseline is absent or invalid, review the full scope. Follow the refresh procedure to review, build, validate, record the new baseline, and rebuild.
- Markdown is the source. Generate HTML from it. Keep standalone pages for subtopics in this order: project structure, diagrams, implemented features, and writing guidance.
- Keep the README's introduction, documentation links, framework structure, and implemented features. Put detailed environments and licensing in the documentation.
- Keep table-of-contents, previous-page, and next-page navigation in place without a page reload, and preserve browser Back. Keep pages to three to five main rules and short examples where practical. Store detailed update history separately.
- Put one item and its version on each environment or version row. Preserve detailed third-party asset lists, versions, and license distinctions in the environment and licensing chapters. Keep Mermaid role descriptions, edge labels, and feature divisions when summarizing diagrams.
- For a request limited to refreshing documentation, avoid unsolicited game-code changes and do not create an automatic commit. Report the results and unverified items.
