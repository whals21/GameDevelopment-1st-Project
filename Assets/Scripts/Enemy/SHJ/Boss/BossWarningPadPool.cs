using System.Collections.Generic;
using UnityEngine;

public class BossWarningPadPool : MonoBehaviour
{
    private Queue<GameObject> pool = new Queue<GameObject>();
    private BossController boss;

    // 컨트롤러에서 주입
    public void Initialize(BossController controller)
    {
        boss = controller;
        CreatePool();
    }

    private void CreatePool()
    {
        pool.Clear();

        
        GameObject prefab = boss.CurrentBulletPrefab;           // 대신 발판 프리팹 가져올 거임
        int count = boss.CurrentBulletCount;                    // 개수도 동일하게

        // 하지만 우리는 발판을 위한 별도 프로퍼티가 필요함 → 아래 주석 참고

        if (prefab == null)
        {
            Debug.LogWarning("[BossWarningPadPool] prefab이 null입니다. BossController에서 발판 프리팹을 제공해주세요.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject pad = Instantiate(prefab, transform);
            pad.SetActive(false);
            pool.Enqueue(pad);
        }
    }

    public GameObject GetPad()
    {
        if (pool.Count == 0)
            return null;

        GameObject pad = pool.Dequeue();
        pad.SetActive(true);
        return pad;
    }

    public void ReturnPad(GameObject pad)
    {
        if (pad == null) return;
        pad.SetActive(false);
        pool.Enqueue(pad);
    }
}