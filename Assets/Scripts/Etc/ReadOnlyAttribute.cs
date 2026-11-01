using UnityEngine;
using System;

// 참고 : https://openlevel.postype.com/post/683234
//[ReadOnly] 혹은[ReadOnly(false)] 로 사용하면 항상 수정할 수 없다.
//[ReadOnly(true)] 로 사용하면 게임이 실행중인 동안에는 수정할 수 없다.

[AttributeUsage(AttributeTargets.Field)]
public class ReadOnlyAttribute : PropertyAttribute
{
    public readonly bool runtimeOnly;

    /// <summary>
    /// 런타임에만 잠글지 항상 잠글지 설정
    /// </summary>
    public ReadOnlyAttribute(bool runtimeOnly = false)
    {
        this.runtimeOnly = runtimeOnly;
    }
}
