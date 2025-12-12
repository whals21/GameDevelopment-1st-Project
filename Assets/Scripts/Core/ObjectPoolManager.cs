using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Prefabs")]
    public Enemy enemyPrefab;
    public EnemyBullet enemyBulletPrefab;  // <<<--- 추가
    public Projectile projectilePrefab;
    public BoomerangProjectile boomerangPrefab;
    public MolotovProjectile molotovPrefab;
    public FireGround fireGroundPrefab;
    public ExpGem expGemPrefab;

    [Header("Pool Sizes")]
    public int enemyPoolSize = 100;
    public int projectilePoolSize = 50;
    public int boomerangPoolSize = 20;
    public int molotovPoolSize = 30;
    public int fireGroundPoolSize = 50;
    public int expGemPoolSize = 200;

    // 풀들
    private ObjectPool<EnemyBullet> enemyBulletPool;// <<<--- 추가
    private ObjectPool<Enemy> enemyPool;
    private ObjectPool<Projectile> projectilePool;
    private ObjectPool<BoomerangProjectile> boomerangPool;
    private ObjectPool<MolotovProjectile> molotovPool;
    private ObjectPool<FireGround> fireGroundPool;
    private ObjectPool<ExpGem> expGemPool;

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
        enemyBulletPool = new ObjectPool<EnemyBullet>(enemyBulletPrefab, enemyPoolSize, transform);
        enemyPool = new ObjectPool<Enemy>(enemyPrefab, enemyPoolSize, transform);
        projectilePool = new ObjectPool<Projectile>(projectilePrefab, projectilePoolSize, transform);
        boomerangPool = new ObjectPool<BoomerangProjectile>(boomerangPrefab, boomerangPoolSize, transform);
        molotovPool = new ObjectPool<MolotovProjectile>(molotovPrefab, molotovPoolSize, transform);
        fireGroundPool = new ObjectPool<FireGround>(fireGroundPrefab, fireGroundPoolSize, transform);
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

    // EnemyBullet 가져오기 및 반환  // <<<--- 추가
    public EnemyBullet GetEnemyBullet()
    {
        return enemyBulletPool.Get();
    }
    public void ReturnEnemyBullet(EnemyBullet bullet)
    {
        enemyBulletPool.Return(bullet);
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

    // Boomerang 가져오기
    public BoomerangProjectile GetBoomerang()
    {
        return boomerangPool.Get();
    }

    public void ReturnBoomerang(BoomerangProjectile boomerang)
    {
        boomerangPool.Return(boomerang);
    }

    // Molotov 가져오기
    public MolotovProjectile GetMolotov()
    {
        return molotovPool.Get();
    }

    public void ReturnMolotov(MolotovProjectile molotov)
    {
        molotovPool.Return(molotov);
    }

    // FireGround 가져오기
    public FireGround GetFireGround()
    {
        return fireGroundPool.Get();
    }

    public void ReturnFireGround(FireGround fireGround)
    {
        fireGroundPool.Return(fireGround);
    }

    // ExpGem 가져오기
    public ExpGem GetExpGem()
    {
        return expGemPool.Get();
    }

    public void ReturnExpGem(ExpGem expGem)
    {
        expGemPool.Return(expGem);
    }

    // 제네릭 방식으로 풀 가져오기
    public ObjectPool<T> GetPool<T>() where T : MonoBehaviour
    {
        if (typeof(T) == typeof(Enemy))
            return enemyPool as ObjectPool<T>;
        else if (typeof(T) == typeof(EnemyBullet))
            return enemyBulletPool as ObjectPool<T>;
        else if (typeof(T) == typeof(Projectile))
            return projectilePool as ObjectPool<T>;
        else if (typeof(T) == typeof(ExpGem))
           return expGemPool as ObjectPool<T>;

        Debug.LogError($"ObjectPoolManager: 풀을 찾을 수 없음: {typeof(T).Name}");
        return null;
    }
}