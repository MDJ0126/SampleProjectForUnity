# Buff 만들기

1. Buff를 상속하고 효과·수명·중복 정책을 정합니다.
2. 필요할 때 UpdateTick을 override합니다.
3. 관리자 소유자 설정 후 AddBuff로 등록합니다.
4. 갱신·종료·제거·구독 해제를 확인합니다.

위치: Assets/Scripts/InGame/Buff. 생성 팩토리를 쓰면 eBuffType과 GameUtils.CreateBuff도 수정합니다.

```csharp
/// <summary>
/// 버프 확장용 뼈대를 제공한다.
/// </summary>
public class ExampleBuff : Buff
{
}
```

부모는 누적 time이 duration에 도달하면 종료를 한 번 알립니다. BuffManager는 역순으로 갱신하여 종료 콜백의 목록 제거를 허용합니다. HealBuff는 남은 시간만 초당10을 회복합니다. 실제 소스 실행으로 작은/큰 deltaTime과 종료 중 제거를 검증했습니다.
