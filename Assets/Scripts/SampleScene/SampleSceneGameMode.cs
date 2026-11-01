using System.Collections;
using Game;

public class SampleSceneGameMode : GameMode
{
    /// <summary>
    /// 씬 전환 완료와 카메라 소유권 확정 뒤 기존 Pawn에 빙의한다.
    /// </summary>
    protected override IEnumerator Start()
    {
        while (SceneControlManager.Instance == null || SceneControlManager.Instance.IsTransitioning || SceneControlManager.Instance.CurrentSceneId != gameObject.scene.name)
        {
            yield return null;
        }
        yield return base.Start();
    }
}
