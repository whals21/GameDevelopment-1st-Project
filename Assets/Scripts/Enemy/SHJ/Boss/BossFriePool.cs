using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossFriePool : MonoBehaviour
{
    private Queue<GameObject> pool = new Queue<GameObject>();
    private BossController boss;

    public void Initialize(BossController controller)
    {
        boss = controller;
        CreatePool();
    }

    void CreatePool()
    {
        pool.Clear();

        GameObject prefab = boss.CurrentBulletPrefab;
        int count = boss.CurrentBulletCount;    //ÃÑ¾Ë Ä«¿ìÆ°

        for (int i = 0; i < count; i++)
        {
            GameObject bullet = Instantiate(prefab, transform); 
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }

    //ÃÑ¾Ë °¡Á®¿Í
    public GameObject GetBullet()
    {
        if (pool.Count == 0) return null;

        GameObject bullet = pool.Dequeue();
        bullet.SetActive(true);
        return bullet;
    }

    //ÃÑ ¹ÝÈ¯
    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        pool.Enqueue(bullet);
    }
}
