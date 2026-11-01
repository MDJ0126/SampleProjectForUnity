using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 바이트 다운로드율과 구분되는 전환 단계 진행도를 표시한다.
    /// </summary>
    public class LoadingUIView : UIView
    {
        public Slider progressBar;
        public Text progressText;

        /// <summary>
        /// 현재 전환 단계와 정규화된 진행도를 함께 갱신한다.
        /// </summary>
        public void SetProgress(float progress, string message)
        {
            progressBar.value = Mathf.Clamp01(progress);
            progressText.text = message + "  " + Mathf.RoundToInt(progressBar.value * 100f) + "%";
        }
    }
}
