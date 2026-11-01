using UnityEngine;
/// <summary>
/// 인스펙터에서 변수 이름을 커스텀 문자열로 표시해주는 어트리뷰트입니다.
/// </summary>
public class DisplayNameAttribute : PropertyAttribute
{
    public string Name { get; private set; }

    /// <summary>
    /// 인스펙터에 대신 표시할 필드 이름 저장
    /// </summary>
    public DisplayNameAttribute(string name)
    {
        this.Name = name;
    }
}
