using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 활성화된 씬의 준비와 실제 게임플레이 시작 시점을 분리한다.
    /// </summary>
    public class SceneContent : MonoBehaviour
    {
        public bool IsGameplayEnabled { get; private set; }

        /// <summary>
        /// 게임플레이 시작 전에 필요한 비동기 초기화를 수행한다.
        /// </summary>
        public virtual Task PrepareAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 상속 씬에서 전환 중 게임플레이를 정지하거나 다시 시작한다.
        /// </summary>
        public virtual void SetGameplayEnabled(bool isEnabled)
        {
            IsGameplayEnabled = isEnabled;
        }

        /// <summary>
        /// 로딩 중 이전 화면을 유지하고 확정 시 카메라·오디오 소유권을 전환한다.
        /// </summary>
        public virtual void SetPresentationEnabled(bool isEnabled)
        {
        }
    }
}
