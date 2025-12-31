using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v2 ObjectPoolManager - 통합 투사체 시스템
///
/// v2 아키텍처를 위한 오브젝트 풀링 관리자입니다.
///
/// 주요 특징:
/// - 통합 Projectile 풀 (ProjectileMovementType으로 모든 동작 지원)
/// - GuardianTop, Drone, LightningStrike, RPGExplosion, FireGround는 별도 풀 유지
/// - 코드 중복 제거 (ValidateAndGetPool<T> 패턴)
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    #region Prefabs
    [Header("Enemy")]
    public Enemy[] enemyPrefabs;
    public int[] enemyPoolSizes;

    [Header("Unified Projectile - v2")]
    public Projectile projectilePrefab;  // 통합 투사체 (모든 타입 지원)

    [Header("Projectiles - Enemy")]
    public EnemyBullet enemyBulletPrefab;

    [Header("Summoned Objects")]
    public GuardianTop guardianTopPrefab;
    public Drone dronePrefab;

    [Header("Effects")]
    public LightningStrike lightningPrefab;
    public FireGround fireGroundPrefab;
    public ExplosionEffect explosionPrefab;

    [Header("Items & UI")]
    public ExpGem expGemPrefab;
    public DamageText damageTextPrefab;
    public RandomBox randomBoxPrefab;
    #endregion

    #region Pool Sizes
    [Header("Pool Sizes")]
    public int projectilePoolSize = 100;     // 통합 투사체 (모든 타입)
    public int enemyBulletPoolSize = 50;
    public int guardianTopPoolSize = 20;
    public int dronePoolSize = 5;
    public int lightningPoolSize = 30;
    public int fireGroundPoolSize = 50;
    public int explosionPoolSize = 30;
    public int expGemPoolSize = 200;
    public int damageTextPoolSize = 50;
    public int randomBoxPoolSize = 10;
    #endregion

    #region Pools
    // 통합 투사체 풀
    private ObjectPool<Projectile> _projectilePool;

    // 적 관련
    private List<ObjectPool<Enemy>> _enemyPools = new List<ObjectPool<Enemy>>();
    private ObjectPool<EnemyBullet> _enemyBulletPool;

    // 소환체 풀
    private ObjectPool<GuardianTop> _guardianTopPool;
    private ObjectPool<Drone> _dronePool;

    // 이펙트 풀
    private ObjectPool<LightningStrike> _lightningPool;
    private ObjectPool<FireGround> _fireGroundPool;
    private ObjectPool<ExplosionEffect> _explosionPool;

    // 아이템/UI 풀
    private ObjectPool<ExpGem> _expGemPool;
    private ObjectPool<DamageText> _damageTextPool;
    private ObjectPool<RandomBox> _randomBoxPool;
    #endregion

    #region Initialization
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("ObjectPoolManager 이미 존재 - 중복 오브젝트 파괴");
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

    private void InitializePools()
    {
        // 통합 투사체 풀
        if (projectilePrefab != null)
        {
            _projectilePool = new ObjectPool<Projectile>(projectilePrefab, projectilePoolSize, transform);
        }

        // 적 풀
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    int size = (enemyPoolSizes != null && i < enemyPoolSizes.Length) ? enemyPoolSizes[i] : 50;
                    _enemyPools.Add(new ObjectPool<Enemy>(enemyPrefabs[i], size, transform));
                }
            }
        }

        // EnemyBullet
        if (enemyBulletPrefab != null)
            _enemyBulletPool = new ObjectPool<EnemyBullet>(enemyBulletPrefab, enemyBulletPoolSize, transform);

        // 소환체
        if (guardianTopPrefab != null)
            _guardianTopPool = new ObjectPool<GuardianTop>(guardianTopPrefab, guardianTopPoolSize, transform);

        if (dronePrefab != null)
            _dronePool = new ObjectPool<Drone>(dronePrefab, dronePoolSize, transform);

        // 이펙트
        if (lightningPrefab != null)
            _lightningPool = new ObjectPool<LightningStrike>(lightningPrefab, lightningPoolSize, transform);

        if (fireGroundPrefab != null)
            _fireGroundPool = new ObjectPool<FireGround>(fireGroundPrefab, fireGroundPoolSize, transform);

        if (explosionPrefab != null)
            _explosionPool = new ObjectPool<ExplosionEffect>(explosionPrefab, explosionPoolSize, transform);

        // 아이템/UI
        if (expGemPrefab != null)
            _expGemPool = new ObjectPool<ExpGem>(expGemPrefab, expGemPoolSize, transform);

        if (damageTextPrefab != null)
            _damageTextPool = new ObjectPool<DamageText>(damageTextPrefab, damageTextPoolSize, transform);

        if (randomBoxPrefab != null)
            _randomBoxPool = new ObjectPool<RandomBox>(randomBoxPrefab, randomBoxPoolSize, transform);
    }
    #endregion

    #region Unified Projectile (v2)
    /// <summary>
    /// 통합 투사체 가져오기
    /// ProjectileMovementType에 따라 모든 투사체 타입을 지원합니다.
    /// </summary>
    public Projectile GetProjectile()
    {
        return ValidateAndGetPool(_projectilePool, "Projectile")?.Get();
    }

    public void ReturnProjectile(Projectile projectile)
    {
        if (projectile == null) return;
        _projectilePool?.Return(projectile);
    }
    #endregion

    #region Enemy
    public Enemy GetEnemy(int index)
    {
        if (index < 0 || index >= _enemyPools.Count) return null;
        if (_enemyPools[index] == null) return null;
        return _enemyPools[index].Get();
    }

    public void ReturnEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        int index = enemy.PoolIndex;
        if (index < 0 || index >= _enemyPools.Count) return;

        _enemyPools[index]?.Return(enemy);
    }
    #endregion

    #region EnemyBullet
    public EnemyBullet GetEnemyBullet()
    {
        return ValidateAndGetPool(_enemyBulletPool, "EnemyBullet")?.Get();
    }

    public void ReturnEnemyBullet(EnemyBullet bullet)
    {
        _enemyBulletPool?.Return(bullet);
    }
    #endregion

    #region GuardianTop
    public GuardianTop GetGuardianTop()
    {
        var top = ValidateAndGetPool(_guardianTopPool, "GuardianTop")?.Get();
        // Get()에서 이미 SetActive(true)됨
        return top;
    }

    public void ReturnGuardianTop(GuardianTop top)
    {
        if (top == null) return;
        // OnDisable에서 ResetForReuse() 자동 호출 (캡슐화 - 전문가 피드백)
        _guardianTopPool?.Return(top);
    }
    #endregion

    #region Drone
    public Drone GetDrone()
    {
        return ValidateAndGetPool(_dronePool, "Drone")?.Get();
    }

    public void ReturnDrone(Drone drone)
    {
        if (drone == null) return;
        drone.Deactivate();
        _dronePool?.Return(drone);
    }
    #endregion

    #region Lightning
    public LightningStrike GetLightning()
    {
        var lightning = ValidateAndGetPool(_lightningPool, "LightningStrike")?.Get();
        if (lightning != null)
        {
            lightning.gameObject.SetActive(true);
        }
        return lightning;
    }

    public void ReturnLightning(LightningStrike lightning)
    {
        if (lightning == null) return;
        lightning.gameObject.SetActive(false);
        _lightningPool?.Return(lightning);
    }
    #endregion

    #region FireGround
    public FireGround GetFireGround()
    {
        var fireGround = ValidateAndGetPool(_fireGroundPool, "FireGround")?.Get();
        // Get()에서 이미 SetActive(true)됨
        return fireGround;
    }

    public void ReturnFireGround(FireGround fireGround)
    {
        if (fireGround == null) return;
        // OnDisable에서 ResetForReuse() 자동 호출 (캡슐화 - 전문가 피드백)
        _fireGroundPool?.Return(fireGround);
    }
    #endregion

    #region Explosion
    public ExplosionEffect GetExplosion()
    {
        var explosion = ValidateAndGetPool(_explosionPool, "ExplosionEffect")?.Get();
        if (explosion != null)
        {
            explosion.gameObject.SetActive(true);
        }
        return explosion;
    }

    public void ReturnExplosion(ExplosionEffect explosion)
    {
        if (explosion == null) return;
        explosion.gameObject.SetActive(false);
        _explosionPool?.Return(explosion);
    }
    #endregion

    #region Items & UI
    public ExpGem GetExpGem()
    {
        return ValidateAndGetPool(_expGemPool, "ExpGem")?.Get();
    }

    public void ReturnExpGem(ExpGem gem)
    {
        _expGemPool?.Return(gem);
    }

    public DamageText GetDamageText()
    {
        return ValidateAndGetPool(_damageTextPool, "DamageText")?.Get();
    }

    public void ReturnDamageText(DamageText text)
    {
        _damageTextPool?.Return(text);
    }

    public RandomBox GetRandomBox()
    {
        return ValidateAndGetPool(_randomBoxPool, "RandomBox")?.Get();
    }

    public void ReturnRandomBox(RandomBox box)
    {
        if (box == null) return;
        box.gameObject.SetActive(false);
        _randomBoxPool?.Return(box);
    }
    #endregion

    #region Utility
    private ObjectPool<T> ValidateAndGetPool<T>(ObjectPool<T> pool, string poolName) where T : MonoBehaviour
    {
        if (pool == null)
        {
            Debug.LogError($"ObjectPoolManager: {poolName} 풀이 초기화되지 않았습니다!");
            return null;
        }
        return pool;
    }

    public ObjectPool<T> GetPool<T>() where T : MonoBehaviour
    {
        if (typeof(T) == typeof(Projectile))
            return _projectilePool as ObjectPool<T>;
        else if (typeof(T) == typeof(Enemy))
            return _enemyPools.Count > 0 ? _enemyPools[0] as ObjectPool<T> : null;
        else if (typeof(T) == typeof(ExpGem))
            return _expGemPool as ObjectPool<T>;

        Debug.LogError($"ObjectPoolManager: 풀을 찾을 수 없음: {typeof(T).Name}");
        return null;
    }
    #endregion
}
