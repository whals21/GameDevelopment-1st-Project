using System.Collections.Generic;
using UnityEngine;

public class BossFirePool : MonoBehaviour
{
    private Queue<GameObject> pool = new Queue<GameObject>();
    private GameObject prefab;
    private BossController boss;
    // ==========================
    // 초기화 (한 번만)
    // ==========================
    public void Initialize(GameObject bulletPrefab, int count, BossController boss)
    {
        prefab = bulletPrefab;
        this.boss = boss;   // ← 이 한 줄이 핵심

        while (pool.Count > 0)
        {
            Destroy(pool.Dequeue());
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);

            BossBullet bullet = obj.GetComponent<BossBullet>();
            bullet.Initialize(this, boss); // ← 이제 null 아님

            pool.Enqueue(obj);
        }

        Debug.Log($"[BossFirePool] Init 완료: {prefab.name} x {count}");
    }

    // ==========================
    // 총알 꺼내기
    // ==========================
    public GameObject GetBullet()
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("[BossFirePool] 총알 부족");
            return null;
        }

        GameObject bullet = pool.Dequeue();
        bullet.SetActive(true);
        return bullet;
    }

    // ==========================
    // 총알 반환
    // ==========================
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;

        bullet.SetActive(false);
        pool.Enqueue(bullet);
    }
}
