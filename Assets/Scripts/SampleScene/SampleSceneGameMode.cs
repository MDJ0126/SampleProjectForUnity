using System.Collections;
using Game;
using UnityEngine;

public class SampleSceneGameMode : GameMode
{
    public AIController opponent;
    private Vector3 _playerPosition;
    private Quaternion _playerRotation;
    private Vector3 _opponentPosition;
    private Quaternion _opponentRotation;
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
        _playerPosition = defaultPawn.transform.position;
        _playerRotation = defaultPawn.transform.rotation;
        if (opponent != null)
        {
            _opponentPosition = opponent.controlledPawn.transform.position;
            _opponentRotation = opponent.controlledPawn.transform.rotation;
        }
    }

    /// <summary>
    /// 배치한 두 Pawn을 복원하여 씬 재로딩 없이 전투를 다시 시작한다.
    /// </summary>
    public void RestartDuel()
    {
        defaultPawn.ResetCombat();
        defaultPawn.transform.SetPositionAndRotation(_playerPosition, _playerRotation);
        playerController.Possess(defaultPawn);
        if (opponent != null)
        {
            Pawn pawn = opponent.controlledPawn;
            pawn.ResetCombat();
            pawn.transform.SetPositionAndRotation(_opponentPosition, _opponentRotation);
            UnityEngine.AI.NavMeshAgent agent = opponent.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent.isOnNavMesh)
            {
                agent.Warp(_opponentPosition);
            }
            opponent.Possess(pawn);
        }
    }
}
