using System.Collections.Generic;
using UnityEngine;
using static EnemyEnum;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Prefabs")]
    public Enemy[] enemyPrefabs; //12/18
    public EnemyBullet enemyBulletPrefab;
    public Projectile projectilePrefab;
    public BoomerangProjectile boomerangPrefab;
    public MolotovProjectile molotovPrefab;
    public BrickProjectile brickPrefab;
    public FireGround fireGroundPrefab;
    public ExpGem expGemPrefab;

    [Header("Pool Sizes")]
    public int[] enemyPoolSizes = new int[] { };    //12/18
    public int enemyBulletPoolSize = 50;
    public int projectilePoolSize = 50;
    public int boomerangPoolSize = 20;
    public int molotovPoolSize = 30;
    public int brickPoolSize = 30;
    public int fireGroundPoolSize = 50;
    public int expGemPoolSize = 200;
    public int expGemPoolSizePerTier = 50; //<---12/18추가 된거임
    // 풀들
    private List<ObjectPool<Enemy>> enemyPools = new List<ObjectPool<Enemy>>(); //12/18
    private ObjectPool<EnemyBullet> enemyBulletPool;
    private ObjectPool<Projectile> projectilePool;
    private ObjectPool<BoomerangProjectile> boomerangPool;
    private ObjectPool<MolotovProjectile> molotovPool;
    private ObjectPool<BrickProjectile> brickPool;
    private ObjectPool<FireGround> fireGroundPool;
    private ObjectPool<ExpGem> expGemPool;

    //12/18추가 되었음
    private List<ObjectPool<ExpGem>> expGemPools = new List<ObjectPool<ExpGem>>();  //리스트 활성화
    private DataManager dataManager;    //경험치 담겨져있음
    private int lastTierCount = 0; // 이전에 생성된 티어 수 추적
    private Dictionary<EnemyObject, ObjectPool<Enemy>> enemyPoolsBySO = new Dictionary<EnemyObject, ObjectPool<Enemy>>();
    [Header("Enemy Objects (SO)")]
    public EnemyObject[] enemyTypes;
    //

    // 유효성 검사 캐시
    private bool isInitialized = false;
    private readonly string[] requiredPrefabs = {
        //"enemyPrefab"//12/18
        "enemyBulletPrefab", "projectilePrefab",
        "boomerangPrefab", "molotovPrefab", "brickPrefab",
        "fireGroundPrefab", "expGemPrefab"
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
        DontDestroyOnLoad(gameObject);
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
        //enemyPool = new ObjectPool<Enemy>(enemyPrefab, enemyPoolSize, transform);//12/18
        enemyBulletPool = new ObjectPool<EnemyBullet>(enemyBulletPrefab, enemyBulletPoolSize, transform);
        projectilePool = new ObjectPool<Projectile>(projectilePrefab, projectilePoolSize, transform);
        boomerangPool = new ObjectPool<BoomerangProjectile>(boomerangPrefab, boomerangPoolSize, transform);
        molotovPool = new ObjectPool<MolotovProjectile>(molotovPrefab, molotovPoolSize, transform);
        brickPool = new ObjectPool<BrickProjectile>(brickPrefab, brickPoolSize, transform);
        fireGroundPool = new ObjectPool<FireGround>(fireGroundPrefab, fireGroundPoolSize, transform);
        expGemPool = new ObjectPool<ExpGem>(expGemPrefab, expGemPoolSize, transform);

        isInitialized = true;


        //12/18
        ExpInitialize();
        EnemysList();
    }

    //12/18
    private void ExpInitialize()    //내가 바로 경험치 그거임
    {
        dataManager = FindObjectOfType<DataManager>();
        if (dataManager == null)
        {
            Debug.LogError("ObjectPoolManager: DataManager를 찾을 수 없습니다!");
            return;
        }
        if (dataManager.expDropObject == null)
        {
            Debug.LogError("ObjectPoolManager: DataManager에 ExpDropObject가 할당되지 않았습니다!");
            return;
        }
        //내용물 확인 안하면 저거 뜬다
        // ExpStart 내용 여기로 이동
        expGemPools.Clear();//청소
        lastTierCount = 0;

        if (expGemPrefab != null)
        {
            expGemPool = new ObjectPool<ExpGem>(expGemPrefab, expGemPoolSize, transform);
        }

        // 티어 풀 생성
        if (dataManager.expDropObject.expGemPrefabs != null)
        {
            foreach (GameObject prefabGO in dataManager.expDropObject.expGemPrefabs)
            {
                if (prefabGO != null)
                {
                    ExpGem prefab = prefabGO.GetComponent<ExpGem>();
                    if (prefab != null)
                    {
                        var pool = new ObjectPool<ExpGem>(prefab, expGemPoolSizePerTier, transform);
                        expGemPools.Add(pool);
                    }
                    else
                    {
                        expGemPools.Add(null);
                    }
                }
                else
                {
                    expGemPools.Add(null);
                }
            }
        }

      
    }

    private void EnemysList()
    {
        enemyPoolsBySO.Clear();

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyPrefabs[i] != null)
            {
                int size = (enemyPoolSizes != null && i < enemyPoolSizes.Length) ? enemyPoolSizes[i] : 50;
                ObjectPool<Enemy> pool = new ObjectPool<Enemy>(enemyPrefabs[i], size, transform);
                enemyPools.Add(pool);

                // SO와 풀 연결
                if (i < enemyTypes.Length)
                    enemyPoolsBySO[enemyTypes[i]] = pool;
            }
        }
    }

    // SO 기반으로 Enemy 가져오기
    public Enemy GetEnemy(EnemyObject so)
    {
        if (!enemyPoolsBySO.ContainsKey(so))
        {
            Debug.LogError("풀 없음: " + so.name);
            return null;
        }

        return enemyPoolsBySO[so].Get();
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
    public Enemy GetEnemy(int index)    //12/19
    {
        if (!isInitialized)
        {
            Debug.LogError("Enemy Pool 아직 초기화 안 됨");
            return null;
        }

        if (index < 0 || index >= enemyPools.Count)
        {
            Debug.LogError($"잘못된 poolIndex: {index}");
            return null;
        }

        if (enemyPools[index] == null)
        {
            Debug.LogError($"enemyPools[{index}] NULL");
            return null;
        }

        return enemyPools[index].Get();
    }

   public void ReturnEnemy(Enemy enemy)//12/19
   {
        if (enemy == null) return;

        int index = enemy.PoolIndex;

        if (index < 0 || index >= enemyPools.Count)
        {
            Debug.LogError($"ReturnEnemy 실패: 잘못된 PoolIndex {index}");
            return;
        }

        enemyPools[index].Return(enemy);
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

    //12/18추가 된 것
    public ExpGem GetExpGem(int tierIndex)
    {
        if (expGemPools == null || tierIndex < 0 || tierIndex >= expGemPools.Count || expGemPools[tierIndex] == null)
        {
            // 티어 풀 없으면 기본 풀 사용 (fallback)
            Debug.LogWarning($"티어 {tierIndex} 풀 없음. 기본 ExpGem 사용");
            return expGemPool.Get();
        }

        return expGemPools[tierIndex].Get();
    }

    public void ReturnExpGem(ExpGem expGem)
    {
        //expGemPool.Return(expGem);

        //12/18 수정된것
        if (expGem == null) return;

        // 티어별 풀에 반환 시도
        if (dataManager != null && dataManager.expDropObject != null && dataManager.expDropObject.expGemPrefabs != null)
        {
            for (int i = 0; i < dataManager.expDropObject.expGemPrefabs.Length; i++)
            {
                if (dataManager.expDropObject.expGemPrefabs[i] != null &&
                    expGem.name.Contains(dataManager.expDropObject.expGemPrefabs[i].name + "(Clone)"))
                {
                    expGemPools[i]?.Return(expGem);
                    return;
                }
            }
        }

        // 못 찾으면 기본 풀에 반환
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
            return enemyPools as ObjectPool<T>; //<12/18
        else if (typeof(T) == typeof(Projectile))
            return projectilePool as ObjectPool<T>;
        else if (typeof(T) == typeof(ExpGem))
           return expGemPool as ObjectPool<T>;

        Debug.LogError($"ObjectPoolManager: 풀을 찾을 수 없음: {typeof(T).Name}");
        return null;
    }
}