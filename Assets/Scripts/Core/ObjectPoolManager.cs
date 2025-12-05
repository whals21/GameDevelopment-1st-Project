using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Prefabs")]
    public Enemy enemyPrefab;
    public Projectile projectilePrefab;
    //public ExpGem expGemPrefab;

    [Header("Pool Sizes")]
    public int enemyPoolSize = 100;
    public int projectilePoolSize = 50;
    public int expGemPoolSize = 200;

    // 풀들
    private ObjectPool<Enemy> enemyPool;
    private ObjectPool<Projectile> projectilePool;
    //private ObjectPool<ExpGem> expGemPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePools();
    }

    private void InitializePools()
    {
        // 각 풀 초기화
        enemyPool = new ObjectPool<Enemy>(enemyPrefab, enemyPoolSize, transform);
        projectilePool = new ObjectPool<Projectile>(projectilePrefab, projectilePoolSize, transform);
        expGemPool = new ObjectPool<ExpGem>(expGemPrefab, expGemPoolSize, transform);
    }

    // Enemy 가져오기
    public Enemy GetEnemy()
    {
        return enemyPool.Get();
    }

    public void ReturnEnemy(Enemy enemy)
    {
        enemyPool.Return(enemy);
    }

    // Projectile 가져오기
    public Projectile GetProjectile()
    {
        return projectilePool.Get();
    }

    public void ReturnProjectile(Projectile projectile)
    {
        projectilePool.Return(projectile);
    }

    // ExpGem 가져오기
    // public ExpGem GetExpGem()
    // {
    //     return expGemPool.Get();
    // }

    // public void ReturnExpGem(ExpGem gem)
    // {
    //     expGemPool.Return(gem);
    // }
}