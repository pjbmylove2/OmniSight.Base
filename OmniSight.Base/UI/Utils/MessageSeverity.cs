namespace OmniSight.Base.UI.Utils
{
    /// <summary>
    /// 시스템 메시지의 심각도를 정의합니다.
    /// </summary>
    public enum MessageSeverity
    {
        /// <summary>일반 정보 (저장, 복원 완료 등)</summary>
        Info,
        /// <summary>경고 (입력값 초과, 통신 지연 등)</summary>
        Warning,
        /// <summary>일반 에러 (파일 쓰기 실패 등)</summary>
        Error,
        /// <summary>크리티컬 알람 / 인터락 (카메라 끊김, PLC 단절, 연속 NG 등) - 상태선 점멸</summary>
        CriticalAlarm
    }
}
