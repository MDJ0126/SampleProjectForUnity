# 다큐먼트 갱신

1. [마지막 기록](11-update-history.md)을 읽고 status를 실행합니다.
2. 이전 SHA → HEAD 차이와 파일 스냅샷 변경을 확인합니다.
3. 변경 파일·영향받는 호출부와 문서만 검토합니다.
4. 빌드·검증 후 record로 완료 기준을 기록합니다.
5. 새 이력을 재빌드·검증합니다. snapshotChanges가 0인지 확인합니다.

```powershell
node docs/documentation/maintenance/refresh.mjs status
node docs/documentation/build.mjs
node docs/documentation/maintenance/validate.mjs
git diff --check
node docs/documentation/maintenance/refresh.mjs record --mode incremental --reviewed --note '변경 요약' --unverified '실행하지 못한 검증'
node docs/documentation/build.mjs
node docs/documentation/maintenance/validate.mjs
node docs/documentation/maintenance/refresh.mjs status
```

- 기준 없음·삭제·이력 분기·범위 변경: 전체 확인 후 --mode full 사용.
- 확인 범위: 자체 소스·텍스트 에셋·설정·문서. 외부 에셋은 변경 영향만 확인.
- 미커밋 변경은 파일 해시로 비교. 상세 상태는 maintenance/update-state.json.
- 원본 수정 후 재빌드하지 않으면 검증 실패. 빌드만으로 SHA를 앞당기지 않음.
- Markdown이 원본이며 빌드 명령으로 HTML·스크립트 검색 내용을 갱신한다. 새 문서는 build.mjs의 목차와 [AGENTS.md](../../AGENTS.md)의 Document Tree에 등록한다.
- 소스 확인과 실행 검증을 구분하고, 테스트하지 않은 동작은 '미확인'으로 표시한다.
- 도구 변경 시 maintenance/verify-refresh.mjs도 실행.
- 게임 코드 임의 수정·자동 커밋 금지. 이력 상세는 maintenance/history-details.jsonl에 누적.

## GitHub Pages 배포

- 사이트: [프로젝트 다큐먼트](https://mdj0126.github.io/SampleProjectForUnity/docs/documentation/index.html).
- 저장소 Settings → Pages → Source를 **GitHub Actions**로 선택한다.
- Deploy from a branch는 사용하지 않는다. 저장소의 오래된 HTML이 자동 생성 문서를 덮어쓸 수 있어 배포 전에 Source를 검사한다.
- main에 문서·자체 소스·설정 변경을 push하면 빌드·검증·배포한다. Actions에서 수동 실행도 가능하다.
- 검색 코드는 마지막 배포 시점의 소스다. 프로젝트 파일 링크는 GitHub에서 연다.
- 최초 배포 완료 전 사이트 접속은 미확인이다.
