using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint; 
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float nextFireTime;


    [Header("Enemies")]
    [SerializeField] private GameObject[] enemies;

    void Update()
    {
        GameObject enemy = FindEnemy();
        if (enemy == null) return;

        // ÃÑ±¸ È¸Àü
        Vector3 dir = enemy.transform.position - transform.position;
        transform.right = dir;

        // ¹ß»ç ÄðÅ¸ÀÓ
        if (Time.time >= nextFireTime)
        {
            Shoot(dir);
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot(Vector3 dir)
    {
        Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.SetDir(dir);
    }

    GameObject FindEnemy()
    {
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var e in enemies)
        {
            if (e == null) continue;
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = e;
            }
        }

        return closest;
    }
}