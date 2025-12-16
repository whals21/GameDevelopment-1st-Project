using System;

[Serializable]
public struct GuardianSkillData
{
    public int level;
    public int topCount;
    public float rotationSpeed;
    public float damageMultiplier;
    public float duration;
    public bool isActive;
    public float remainingTime;

    public string GetStatusText()
    {
        if (isActive)
        {
            if (remainingTime < 0)
                return $"가디언 Lv.{level} (영구 활성화)";
            else
                return $"가디언 Lv.{level} (남은 시간: {remainingTime:F1}초)";
        }
        else
        {
            return $"가디언 Lv.{level} (비활성화)";
        }
    }

    public string GetStatsText()
    {
        return $"톱날: {topCount}개\n" +
               $"회전 속도: {rotationSpeed:F0}°/s\n" +
               $"데미지 배수: x{damageMultiplier:F1}\n" +
               $"지속시간: {duration:F0}초";
    }
}