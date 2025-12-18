using UnityEngine;
using System.Collections;

public class DroneSkill : MonoBehaviour
{
    [Header("드론 설정")]
    [SerializeField] private GameObject dronePrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform droneFollowPoint;  // 드론이 따라다닐 위치

    // 스킬 데이터 참조
    private SkillData skillData;

    [Header("레벨별 기본 설정")]
    [SerializeField] private int baseMissileCount = 10;  // 3 → 10으로 증가
    [SerializeField] private float baseCooldown = 3f;
    [SerializeField] private float baseDamage = 8f;
    [SerializeField] private float baseSpeed = 15f;

    [Header("발사 설정")]
    [SerializeField] private float fireRate = 30f;  // 발사 방향 회전 속도 (도/초)
    [SerializeField] private float missileSpread = 75f;  // 30도 → 75도로 확장
    [SerializeField] private float followDistance = 1.2f;  // 플레이어와의 거리 (기본값 1.2)
    [SerializeField] private float followSpeed = 2.5f;  // 추적 속도 (기본값 2.5)

    // 상태 변수
    private int currentLevel = 1;
    private Drone currentDrone;
    private bool isActive = false;
    private float currentFireAngle = 0f;
    private Coroutine fireCoroutine;

    public void InitializeDroneSkill(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, 5);

        if (currentDrone == null)
        {
            SpawnDrone();
            UpdateDroneStats();
            ActivateDrone();
        }
        else
        {
            // 이미 드론이 있으면 레벨만 업데이트
            UpdateDroneStats();
        }
    }

    // SkillManager에서 스킬 데이터 설정
    public void SetSkillData(SkillData data)
    {
        skillData = data;

        // SkillData 값으로 기본 설정 업데이트
        if (data != null)
        {
            // 기본 스탯 값으로 설정
            baseMissileCount = data.projectileCount;
            baseCooldown = data.cooldown;
            baseDamage = data.damage;
            baseSpeed = data.projectileSpeed;

            Debug.Log($"DroneSkill SetSkillData - 미사일 수: {baseMissileCount}, 데미지: {baseDamage}");
            Debug.Log($"드론 스킬 기본 스탯 업데이트 - 쿨타임: {baseCooldown}, 속도: {baseSpeed}");
        }
    }

    private void SpawnDrone()
    {
        // 오브젝트 풀에서 드론 가져오기
        currentDrone = ObjectPoolManager.Instance.GetDrone();
        if (currentDrone == null)
        {
            GameObject droneObj = Instantiate(dronePrefab, transform);
            currentDrone = droneObj.GetComponent<Drone>();
        }
        else
        {
            currentDrone.gameObject.SetActive(true);
        }

        // 드론 초기 위치 설정
        Vector3 startPos = playerTransform.position + Vector3.up * followDistance;
        currentDrone.transform.position = startPos;
        currentDrone.Initialize(playerTransform, followDistance, followSpeed);
    }

    private void UpdateDroneStats()
    {
        if (currentDrone == null) return;

        // 레벨별 스탯 계산
        float damage = (baseDamage + (currentLevel - 1) * 2f) * 0.5f;  // 개당 데미지 50% 조정 (전체 데미지 유지)
        float speed = baseSpeed * 1.3f;  // 속도 30% 증가 (15 → 19.5)
        int missileCount = baseMissileCount + (currentLevel - 1) * 2;  // 레벨당 +2 미사일
        float cooldown = Mathf.Max(0.5f, baseCooldown - (currentLevel - 1) * 0.2f);  // 레벨당 -0.2초 쿨타임

        currentDrone.SetStats(damage, speed, missileCount, cooldown);
    }

    private void ActivateDrone()
    {
        if (currentDrone == null) return;

        isActive = true;
        currentDrone.Activate();

        // 미사일 발사 코루틴 시작
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
        }
        fireCoroutine = StartCoroutine(FireMissilesRoutine());
    }

    private IEnumerator FireMissilesRoutine()
    {
        Debug.Log("미사일 발사 코루틴 시작! isActive: " + isActive);

        while (isActive && currentDrone != null)
        {
            // 쿨타임 계산
            float cooldown = Mathf.Max(0.5f, baseCooldown - (currentLevel - 1) * 0.2f);
            Debug.Log("미사일 발사 대기 중... 쿨다운: " + cooldown + "초");
            yield return new WaitForSeconds(cooldown);

            if (!isActive || currentDrone == null) break;

            Debug.Log("미사일 다발 발사!");
            // 미사일 발사
            FireMissileBurst();
        }
    }

    private void FireMissileBurst()
    {
        if (currentDrone == null) return;

        // 레벨별 스탯 계산
        int missileCount = baseMissileCount + (currentLevel - 1) * 2;
        float damage = (baseDamage + (currentLevel - 1) * 2f) * 0.5f;  // 개당 데미지 50% 조정
        float speed = baseSpeed * 1.3f;  // 속도 30% 증가
        float cooldown = Mathf.Max(0.5f, baseCooldown - (currentLevel - 1) * 0.2f);

        // 현재 발사 각도 업데이트
        currentFireAngle += fireRate * cooldown;
        if (currentFireAngle >= 360f) currentFireAngle -= 360f;

        // 그룹별 미사일 발사 시작
        StartCoroutine(FireMissileGroups(missileCount, damage, speed));
    }

    private IEnumerator FireMissileGroups(int totalMissiles, float damage, float speed)
    {
        // 그룹별 미사일 수 (3-4발씩)
        int groupSize = Mathf.Clamp(4, 3, totalMissiles);  // 최대 4발씩
        int remainingMissiles = totalMissiles;

        // 미사일 다발 발사 각도 계산
        float angleStep = missileSpread / (totalMissiles - 1);
        float startAngle = currentFireAngle - (missileSpread / 2f);
        int currentMissileIndex = 0;

        while (remainingMissiles > 0)
        {
            int missilesInThisGroup = Mathf.Min(groupSize, remainingMissiles);

            // 그룹 내 미사일 발사
            for (int i = 0; i < missilesInThisGroup; i++)
            {
                float angle = startAngle + (angleStep * currentMissileIndex);
                Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;

                SpawnMissile(currentDrone.transform.position, direction, damage, speed);
                currentMissileIndex++;
                remainingMissiles--;

                // 미사일 간 짧은 지연 (20ms)
                if (i < missilesInThisGroup - 1)  // 그룹의 마지막 미사일이 아닐 경우에만
                {
                    yield return new WaitForSeconds(0.02f);  // 20ms 지연
                }
            }

            // 그룹 간 지연 (80ms)
            if (remainingMissiles > 0)
            {
                yield return new WaitForSeconds(0.08f);  // 80ms 지연
            }
        }
    }

    private void SpawnMissile(Vector3 position, Vector2 direction, float damage, float speed)
    {
        // 오브젝트 풀에서 미사일 가져오기
        var missile = ObjectPoolManager.Instance.GetMissile();
        if (missile == null)
        {
            GameObject missileObj = new GameObject("Missile");
            missileObj.transform.SetParent(transform);
            missile = missileObj.AddComponent<MissileProjectile>();
        }
        else
        {
            missile.gameObject.SetActive(true);
            missile.transform.position = position;

            // 미사일 상태 초기화 (중요!)
            MissileProjectile missileComp = missile as MissileProjectile;
            if (missileComp != null)
            {
                missileComp.ResetForReuse();
            }
        }

        // 수동으로 초기화 (Initialize 메서드가 없으므로)
        missile.transform.position = position;
        missile.Init(damage, speed, direction);
        missile.gameObject.transform.localScale = Vector3.one * 0.3f;  // 미사일 크기를 30%로大幅 줄임

        // 미사일 방향 설정 (발사 방향으로 회전)
        MissileProjectile missileProjectile = missile as MissileProjectile;
        if (missileProjectile != null)
        {
            missileProjectile.SetDirection(direction);
        }
    }

    public void DeactivateDroneSkill()
    {
        isActive = false;

        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }

        if (currentDrone != null)
        {
            currentDrone.Deactivate();
            ObjectPoolManager.Instance.ReturnDrone(currentDrone);
            currentDrone = null;
        }
    }

    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;

        // 드론 추적 포인트 설정
        if (droneFollowPoint == null)
        {
            GameObject followPoint = new GameObject("DroneFollowPoint");
            followPoint.transform.SetParent(playerTransform);
            followPoint.transform.localPosition = Vector3.up * followDistance;
            droneFollowPoint = followPoint.transform;
        }
    }
}