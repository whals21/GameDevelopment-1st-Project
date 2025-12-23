using System.Collections.Generic;
using UnityEngine;

public class BossWarningPadPool : MonoBehaviour
{
    [System.Serializable]
    public class PadInfo
    {
        public GameObject prefab; // 사용할 발판 프리팹
        public int count;         // 최대 개수
    }

    public PadInfo[] pads;                       // Inspector에서 세팅
    private List<GameObject>[] padPools;         // 풀링 저장

    private void Awake()
    {
        padPools = new List<GameObject>[pads.Length];

        for (int i = 0; i < pads.Length; i++)
        {
            padPools[i] = new List<GameObject>();
            for (int j = 0; j < pads[i].count; j++)
            {
                GameObject pad = Instantiate(pads[i].prefab, transform);
                pad.SetActive(false);
                padPools[i].Add(pad);
            }
        }
    }

    // 사용 가능한 발판 가져오기
    public GameObject GetPad(int type)
    {
        foreach (var pad in padPools[type])
        {
            if (!pad.activeInHierarchy)
            {
                pad.SetActive(true);
                return pad;
            }
        }
        return null; // 다 사용 중이면 null
    }

    // 발판 반환
    public void ReturnPad(GameObject pad)
    {
        pad.SetActive(false);
    }
}