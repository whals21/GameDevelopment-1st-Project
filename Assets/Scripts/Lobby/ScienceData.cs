using System.Collections.Generic;

[System.Serializable]
public class ScienceData
{
    public int gold;            // 돈
    public int topRowLevel;     // 윗줄 레벨 (0부터 시작)
    public List<int> purchasedSpecials; // 산 특수 스킬(아랫줄) 번호들

    public ScienceData()
    {
        gold = 1000; // 테스트용 1000원
        topRowLevel = 0;
        purchasedSpecials = new List<int>();
    }
}