using System;

/// <summary>
/// 무작위 초기 위상과 누적으로 성공 횟수와 기대 횟수의 차이를 1 미만으로 유지한다.
/// </summary>
public sealed class DistributedToggle
{
    private double _remainder = UnityEngine.Random.Range(0, 16777216) / 16777216.0;

    /// <summary>
    /// 0~1 확률을 누적해 판정하며 범위 밖 값과 NaN은 예외로 거부한다.
    /// </summary>
    public bool Next(float probability)
    {
        if (float.IsNaN(probability) || probability < 0f || probability > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(probability));
        }

        _remainder += probability;
        if (_remainder >= 1.0)
        {
            _remainder -= 1.0;
            return true;
        }

        return false;
    }
}
