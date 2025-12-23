using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // 저장
    public int level;
    public float currentExp;
    public float maxExp;
    public int killCount;

    // 초기화
    public GameData()
    {
        level = 1;
        currentExp = 0;
        maxExp = 10;
        killCount = 0;
    }
}