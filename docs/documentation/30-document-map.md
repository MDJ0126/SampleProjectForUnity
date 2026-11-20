# 문서 관계도

프로젝트 루트의 AGENTS.md에서 소개와 주제별 Markdown 문서로 이어집니다. 노드를 누르면 현재 페이지에서 본문을 읽을 수 있습니다.

첫 단계 실선은 시작 문서에서 이어지는 주제, 아래 점선은 주제별로 함께 읽을 문서입니다. 예정으로 표시한 문서는 해당 페이지에서 구현 상태를 확인하세요.

## 문서 연결

- [AGENTS](../../AGENTS.md) — Project instructions
  - [프로젝트 소개](00-start.md) — 다큐먼트 안내
  - [프로젝트 구조](01-project.md) — 폴더와 공통 시스템
    - [환경과 라이선스](09-environment.md) — Unity 버전과 외부 에셋
  - [작업과 검증](06-workflow.md) — 시작·변경·검증 순서
    - [Unity 작업 규칙](04-unity.md) — 에디터와 에셋 작업 기준
    - [예제와 문서 양식](05-examples.md) — 예제 확인과 작성 양식
  - [전체 프로세스](21-process-diagrams.md) — 상위 흐름 문서 · 예정
    - [인게임 연결](07-diagrams.md) — 런타임 구성 관계
      - [캐릭터 다이어그램](22-character-diagrams.md) — 캐릭터와 제어 계층
      - [아웃게임 흐름](20-outgame-diagrams.md) — 아웃게임 문서 · 예정
  - [구현 기능](08-features.md) — 기능과 소스의 진입점
    - [씬 전환과 UI](26-developed-scene-ui.md) — 초기화와 화면 전환
    - [공통 전투](27-developed-combat.md) — 무기·장비·AI
      - [캐릭터와 제어](23-developed-character.md) — 캐릭터·빙의·입력
        - [HUD 구현](24-developed-hud.md) — 화면 상태와 표시
          - [HUD 작성](16-hud.md) — 표시 계층과 작성 규칙
      - [버프 작성](14-buff.md) — 상태 효과 작성
      - [스킬 작성](15-skill.md) — 스킬 계약과 흐름
    - [유틸리티와 에디터](25-developed-tools.md) — 개발 도구와 공용 모듈
      - [에이전트 모델](28-agent-models.md) — 모델 요청과 게임별 해석
      - [WAV 인코딩](29-wave-encoding.md) — 오디오 입력의 공용 처리
  - [네이밍 컨벤션](02-naming.md) — 이름과 코드 형식
    - [클래스 작성](12-class.md) — 역할과 상태 관리
      - [구조체 작성](13-struct.md) — 값 묶음과 복사 계약
      - [열거형 작성](18-enum.md) — 상태와 직렬화 값
    - [함수 작성](03-functions.md) — 입력·실패·상태 변경
      - [주석과 줄바꿈](17-comments-format.md) — 주석과 제어문 형식
      - [이벤트 작성](19-events.md) — 발행·구독·해제
  - [문서 갱신](10-updating.md) — 원본·검증·배포
    - [갱신 이력](11-update-history.md) — 검토 기준과 변경 기록
