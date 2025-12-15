using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Prefabs")]
    public Enemy enemyPrefab;
    public EnemyBullet enemyBulletPrefab;
    public Projectile projectilePrefab;
    public BoomerangProjectile boomerangPrefab;
    public MolotovProjectile molotovPrefab;
    public BrickProjectile brickPrefab;
    public FireGround fireGroundPrefab;
    public ExpGem expGemPrefab;
    public DamageText damageTextPrefab;

    [Header("Pool Sizes")]
    public int enemyPoolSize = 100;
    public int enemyBulletPoolSize = 50;
    public int projectilePoolSize = 50;
    public int boomerangPoolSize = 20;
    public int molotovPoolSize = 30;
    public int brickPoolSize = 30;
    public int fireGroundPoolSize = 50;
    public int expGemPoolSize = 200;
    public int damageTextPoolSize = 50;

    // 풀들
    private ObjectPool<Enemy> enemyPool;
    private ObjectPool<EnemyBullet> enemyBulletPool;
    private ObjectPool<Projectile> projectilePool;
    private ObjectPool<BoomerangProjectile> boomerangPool;
    private ObjectPool<MolotovProjectile> molotovPool;
    private ObjectPool<BrickProjectile> brickPool;
    private ObjectPool<FireGround> fireGroundPool;
    private ObjectPool<ExpGem> expGemPool;
    private ObjectPool<DamageText> damageTextPool;

    // 유효성 검사 캐시
    private bool isInitialized = false;
    private readonly string[] requiredPrefabs = {
        "enemyPrefab", "enemyBulletPrefab", "projectilePrefab",
        "boomerangPrefab", "molotovPrefab", "brickPrefab",
        "fireGroundPrefab", "expGemPrefab", "damageTextPrefab"
    };

    private void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializePools();
    }

    // 풀 초기화 분리
    private void InitializePools()
    {
        ValidateRequiredPrefabs();

        // 풀 초기화
        enemyPool = new ObjectPool<Enemy>(enemyPrefab, enemyPoolSize, transform);
        enemyBulletPool = new ObjectPool<EnemyBullet>(enemyBulletPrefab, enemyBulletPoolSize, transform);
        projectilePool = new ObjectPool<Projectile>(projectilePrefab, projectilePoolSize, transform);
        boomerangPool = new ObjectPool<BoomerangProjectile>(boomerangPrefab, boomerangPoolSize, transform);
        molotovPool = new ObjectPool<MolotovProjectile>(molotovPrefab, molotovPoolSize, transform);
        brickPool = new ObjectPool<BrickProjectile>(brickPrefab, brickPoolSize, transform);
        fireGroundPool = new ObjectPool<FireGround>(fireGroundPrefab, fireGroundPoolSize, transform);
        expGemPool = new ObjectPool<ExpGem>(expGemPrefab, expGemPoolSize, transform);
        damageTextPool = new ObjectPool<DamageText>(damageTextPrefab, damageTextPoolSize, transform);

        isInitialized = true;
    }

    // 필수 프리팹 유효성 검사
    private void ValidateRequiredPrefabs()
    {
        foreach (string prefabName in requiredPrefabs)
        {
            var prefab = GetType().GetField(prefabName)?.GetValue(this);
            if (prefab == null)
            {
                Debug.LogError($"ObjectPoolManager: {prefabName}이 설정되지 않았습니다!");
            }
        }
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

    // Brick 가져오기
    public BrickProjectile GetBrick()
    {
        return brickPool.Get();
    }

    public void ReturnBrick(BrickProjectile brick)
    {
        brickPool.Return(brick);
    }

    // FireGround 가져오기
    public FireGround GetFireGround()
    {
        if (!ValidatePoolInitialized(fireGroundPool, "FireGround")) return null;
        return fireGroundPool.Get();
    }

    public void ReturnFireGround(FireGround fireGround)
    {
        if (!ValidatePoolInitialized(fireGroundPool, "FireGround")) return;
        fireGroundPool.Return(fireGround);
    }

    // ExpGem 가져오기
    public ExpGem GetExpGem()
    {
        return expGemPool.Get();
    }
        
    // DamageText 가져오기 및 반환
    public DamageText GetDamageText()
    {
        return damageTextPool.Get();
    }
    public void ReturnDamageText(DamageText text)
    {
        damageTextPool.Return(text);
    }

    public void ReturnExpGem(ExpGem expGem)
    {
        expGemPool.Return(expGem);
    }

    // EnemyBullet 가져오기 및 반환 (팀원 코드 지원용)
    public EnemyBullet GetEnemyBullet()
    {
        return enemyBulletPool.Get();
    }

    public void ReturnEnemyBullet(EnemyBullet enemyBullet)
    {
        enemyBulletPool.Return(enemyBullet);
    }

    // 풀 초기화 상태 유효성 검사
    private bool ValidatePoolInitialized<T>(ObjectPool<T> pool, string poolName) where T : MonoBehaviour
    {
        if (!isInitialized)
        {
            Debug.LogError($"ObjectPoolManager: {poolName} 풀이 초기화되지 않았습니다!");
            return false;
        }

        if (pool == null)
        {
            Debug.LogError($"ObjectPoolManager: {poolName} 풀이 null입니다!");
            return false;
        }

        return true;
    }

    // 제네릭 방식으로 풀 가져오기
    public ObjectPool<T> GetPool<T>() where T : MonoBehaviour
    {
        if (typeof(T) == typeof(Enemy))
            return enemyPool as ObjectPool<T>;
        else if (typeof(T) == typeof(Projectile))
            return projectilePool as ObjectPool<T>;
        else if (typeof(T) == typeof(ExpGem))
           return expGemPool as ObjectPool<T>;

        Debug.LogError($"ObjectPoolManager: 풀을 찾을 수 없음: {typeof(T).Name}");
        return null;
    }
}