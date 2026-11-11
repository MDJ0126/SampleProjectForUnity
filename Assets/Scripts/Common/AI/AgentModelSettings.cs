using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// 여러 동료가 공유하는 모델 연결과 호출 예산을 저장하고 인증키는 환경변수에서 읽게 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Sample Project/AI/Model Settings")]
    public sealed class AgentModelSettings : ScriptableObject
    {
        public bool useModel;
        public string endpoint = "http://127.0.0.1:11434/v1/chat/completions";
        public string model = "qwen2.5:0.5b";
        public string apiKeyEnvironmentVariable = "GAME_AGENT_API_KEY";
        [Min(1)] public int maxConcurrentRequests = 2;
        [Min(1)] public int timeoutSeconds = 60;
        [Min(0.2f)] public float decisionInterval = 3f;
        [Min(0.5f)] public float maxDecisionAge = 8f;
        [Min(1f)] public float failureBackoff = 10f;
        [Min(64)] public int maxTokens = 160;
        [TextArea] public string personality = "Follow the task instructions supplied by the caller.";
    }
}
