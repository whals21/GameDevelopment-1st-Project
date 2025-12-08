using UnityEngine;

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
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 풀 초기화
        enemyPool = new ObjectPool<Enemy>(enemyPrefab, enemyPoolSize, transform);
        projectilePool = new ObjectPool<Projectile>(projectilePrefab, projectilePoolSize, transform);
        //expGemPool = new ObjectPool<ExpGem>(expGemPrefab, expGemPoolSize, transform);
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

    // ExpGem 가져오기 (Week 1 Day 4-5)
    // public ExpGem GetExpGem()
    // {
    //     return expGemPool.Get();
    // }
    //
    // public void ReturnExpGem(ExpGem expGem)
    // {
    //     expGemPool.Return(expGem);
    // }

    // 제네릭 방식으로 풀 가져오기
    public ObjectPool<T> GetPool<T>() where T : MonoBehaviour
    {
        if (typeof(T) == typeof(Enemy))
            return enemyPool as ObjectPool<T>;
        else if (typeof(T) == typeof(Projectile))
            return projectilePool as ObjectPool<T>;
        //else if (typeof(T) == typeof(ExpGem))
        //    return expGemPool as ObjectPool<T>;

        Debug.LogError($"ObjectPoolManager: 풀을 찾을 수 없음: {typeof(T).Name}");
        return null;
    }
}