using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.AI
{
    /// <summary>
    /// 호환 채팅 요청을 만들고 공유 설정별 동시 호출 슬롯을 제한한다.
    /// </summary>
    public static class AgentModelClient
    {
        private static readonly Dictionary<AgentModelSettings, int> _active = new();

        [Serializable] private sealed class Message
        {
            public string role;
            public string content;
        }

        [Serializable] private sealed class Request
        {
            public string model;
            public Message[] messages;
            public bool stream;
            public int max_tokens;
            public ResponseFormat response_format = new();
        }

        [Serializable] private sealed class ResponseFormat
        {
            public string type = "json_object";
        }

        [Serializable] private sealed class Choice
        {
            public Message message;
        }

        [Serializable] private sealed class Response
        {
            public Choice[] choices;
        }

        /// <summary>
        /// 도메인 재로드를 생략한 플레이 세션에서도 이전 호출 수를 비운다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _active.Clear();

        /// <summary>
        /// 같은 설정을 사용하는 에이전트의 요청 슬롯을 하나 확보한다.
        /// </summary>
        public static bool TryAcquire(AgentModelSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            _active.TryGetValue(settings, out int count);
            if (count >= Mathf.Max(1, settings.maxConcurrentRequests))
            {
                return false;
            }
            _active[settings] = count + 1;
            return true;
        }

        /// <summary>
        /// 성공·실패·취소 여부와 무관하게 확보했던 호출 슬롯을 반환한다.
        /// </summary>
        public static void Release(AgentModelSettings settings)
        {
            if (!ReferenceEquals(settings, null) && _active.TryGetValue(settings, out int count))
            {
                if (count <= 1)
                {
                    _active.Remove(settings);
                }
                else
                {
                    _active[settings] = count - 1;
                }
            }
        }

        /// <summary>
        /// 로컬 HTTP 또는 원격 HTTPS 주소에 제한된 행동 선택을 요청한다.
        /// </summary>
        public static UnityWebRequest CreateRequest(AgentModelSettings settings, string systemPrompt, string observation)
        {
            if (!Uri.TryCreate(settings.endpoint, UriKind.Absolute, out Uri uri) ||
                !(uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            {
                throw new ArgumentException("Use HTTPS or a loopback HTTP endpoint.");
            }

            var body = new Request
            {
                model = settings.model,
                max_tokens = Mathf.Clamp(settings.maxTokens, 64, 2048),
                stream = false,
                messages = new[]
                {
                    new Message
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new Message { role = "user", content = observation }
                }
            };
            var request = new UnityWebRequest(uri.AbsoluteUri, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body))),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Mathf.Clamp(settings.timeoutSeconds, 1, 120),
                redirectLimit = 0
            };
            request.SetRequestHeader("Content-Type", "application/json");
            string variable = settings.apiKeyEnvironmentVariable;
            string key = string.IsNullOrWhiteSpace(variable) ? null : Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(key))
            {
                request.SetRequestHeader("Authorization", "Bearer " + key);
            }
            return request;
        }

        /// <summary>
        /// 첫 응답 선택지의 텍스트를 반환하며 게임별 행동 해석은 호출자에게 맡긴다.
        /// </summary>
        public static string ParseContent(string json)
        {
            var response = JsonUtility.FromJson<Response>(json);
            if (response?.choices == null || response.choices.Length == 0 || response.choices[0]?.message == null)
            {
                throw new FormatException("Missing model response.");
            }
            string content = response.choices[0].message.content?.Trim();
            if (string.IsNullOrEmpty(content) || !content.StartsWith("{") || !content.EndsWith("}"))
            {
                throw new FormatException("Expected one JSON object.");
            }
            return content;
        }
    }

}
