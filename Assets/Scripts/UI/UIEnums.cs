namespace Game
{
    /// <summary>
    /// 화면과 모달 및 전역 오버레이의 표시 순서를 구분한다.
    /// </summary>
    public enum eUILayer
    {
        None = 0,
        Screen = 1,
        Popup = 2,
        Overlay = 3,
    }

    /// <summary>
    /// 씬 전환 시 정리할 UI와 전역으로 유지할 UI를 구분한다.
    /// </summary>
    public enum eUILifetime
    {
        None = 0,
        Scene = 1,
        Global = 2,
    }
}
