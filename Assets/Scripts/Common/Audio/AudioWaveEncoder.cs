using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// 게임별 입력·통신·UI에 의존하지 않고 Unity 오디오 샘플을 표준 PCM WAV로 변환한다.
    /// </summary>
    public static class AudioWaveEncoder
    {
        /// <summary>
        /// 16kHz 클립의 유효 샘플을 모노 PCM16 WAV로 반환하며 원본 클립과 디스크를 변경하지 않는다.
        /// </summary>
        public static byte[] EncodePcm16Mono(AudioClip clip, int samples)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }
            if (clip.frequency != 16000 || samples < 1 || samples > clip.samples)
            {
                throw new ArgumentException("16kHz 클립의 유효한 샘플 수가 필요합니다.");
            }
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0))
            {
                throw new InvalidOperationException("읽을 수 있는 오디오 데이터가 필요합니다.");
            }
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(16000);
            writer.Write(32000);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(samples * 2);
            for (int index = 0; index < samples; index++)
            {
                float mono = 0f;
                for (int channel = 0; channel < clip.channels; channel++)
                {
                    mono += data[index * clip.channels + channel];
                }
                float normalized = mono / clip.channels;
                if (float.IsNaN(normalized) || float.IsInfinity(normalized))
                {
                    throw new ArgumentException("유한한 오디오 샘플이 필요합니다.");
                }
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(normalized, -1f, 1f) * short.MaxValue));
            }
            return stream.ToArray();
        }
    }
}
