using System;

/// <summary>
/// 인덱스를 묶음마다 한 번씩 선택하고 묶음 경계의 연속 중복을 방지한다.
/// </summary>
public sealed class ShuffleBag
{
    private int[] _indices = Array.Empty<int>();
    private int _remaining;
    private int _previous = -1;

    /// <summary>
    /// 양수인 후보 수에서 인덱스를 선택하며 수가 바뀌면 새 묶음을 시작한다.
    /// </summary>
    public int Next(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (_indices.Length != count)
        {
            _indices = new int[count];
            _remaining = 0;
        }

        if (_remaining == 0)
        {
            for (int i = 0; i < count; i++)
            {
                _indices[i] = i;
            }

            for (int i = count - 1; i > 0; i--)
            {
                int index = UnityEngine.Random.Range(0, i + 1);
                int value = _indices[i];
                _indices[i] = _indices[index];
                _indices[index] = value;
            }

            // 끝에서 꺼내므로 첫 선택이 이전 결과와 같으면 다른 위치와 교환한다.
            if (count > 1 && _indices[count - 1] == _previous)
            {
                int index = UnityEngine.Random.Range(0, count - 1);
                _indices[count - 1] = _indices[index];
                _indices[index] = _previous;
            }

            _remaining = count;
        }

        _previous = _indices[--_remaining];
        return _previous;
    }
}
