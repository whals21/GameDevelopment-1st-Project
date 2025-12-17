using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GuardianSkill : MonoBehaviour
{
    [Header("가디언 설정")]
    [SerializeField] private GameObject topPrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float reactivateDelay = 1f;

    [Header("레벨별 설정")]
    [SerializeField] private int baseTopCount = 2;
    [SerializeField] private float baseRadius = 2f;
    [SerializeField] private float baseRotationSpeed = 180f;
    [SerializeField] private float baseDuration = 5f;
    [SerializeField] private float baseDamage = 15f;
    [SerializeField] private float baseKnockback = 5f;

    // 레벨별 스탯 배열
    private readonly float[] damageMultipliers = {0.5f, 0.6f, 0.7f, 0.8f, 0.9f};
    [SerializeField] private float[] speedMultipliers = {1f, 1.2f, 1.4f, 1.6f, 1.8f}; // 인스펙터에서 조절 가능

    // 레벨 관리
    private int currentLevel = 1;
    private int maxLevel = 5;

    // 런타임 데이터
    private int currentTopCount;
    private float currentRotationSpeed;
    private float currentDamageMultiplier;
    private float currentDuration;
    private bool isEvolved = false;

    // 톱날 관리
    private List<GuardianTop> activeTops = new List<GuardianTop>();
    private bool isActive = false;
    private float activeTimer = 0f;
    private Coroutine activationCoroutine = null;

    // 오디오
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip levelUpSound;
    private AudioSource audioSource;

    // 이펙트
    [SerializeField] private GameObject activationEffect;
    [SerializeField] private GameObject levelUpEffect;

    // 상수
    private const float UPGRADE_EFFECT_DURATION = 1f;

    private void Awake()
    {
        // 오디오 소스 초기화
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 플레이어 자동 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    private void Start()
    {
        // 초기 레벨 설정
        InitializeLevel();
    }

    private void Update()
    {
        if (!isActive) return;

        // 타이머 업데이트 (진화 전만)
        if (!isEvolved)
        {
            activeTimer += Time.deltaTime;

            if (activeTimer >= currentDuration)
            {
                DeactivateGuardian();
            }
        }
    }

    // 초기 레벨 설정
    private void InitializeLevel()
    {
        UpdateLevelStats();
        InitializeTops();
    }

    // 레벨별 스탯 업데이트
    private void UpdateLevelStats()
    {
        // 톱날 수 계산
        currentTopCount = baseTopCount + (currentLevel - 1);

        // 회전 속도 계산
        currentRotationSpeed = baseRotationSpeed * speedMultipliers[currentLevel - 1];

        // 데미지 배수 설정
        currentDamageMultiplier = damageMultipliers[Mathf.Min(currentLevel - 1, damageMultipliers.Length - 1)];

        // 지속시간 설정 (레벨당 1초 증가)
        currentDuration = baseDuration + (currentLevel - 1);
    }

    // 톱날 초기화
    private void InitializeTops()
    {
        // 기존 톱날 정리
        ClearAllTops();

        // 새 톱날 생성
        float angleStep = 360f / currentTopCount;

        for (int i = 0; i < currentTopCount; i++)
        {
            // 오브젝트 풀에서 톱날 가져오기
            GuardianTop top = ObjectPoolManager.Instance.GetGuardianTop();
            if (top == null)
            {
                Debug.LogWarning("Guardian: 톱날을 풀에서 가져오지 못했습니다!");
                continue;
            }

            // 초기 각도 균등 분배
            float initialAngle = i * angleStep;

            // 톱날 초기화
            top.Initialize(
                initialAngle,
                baseRadius,
                currentRotationSpeed,
                playerTransform,
                currentDamageMultiplier
            );

            // 스탯 업데이트
            top.UpdateStats(baseDamage, baseKnockback, currentRotationSpeed);

            activeTops.Add(top);
        }
    }

    // 가디언 활성화
    public void ActivateGuardian()
    {
        if (isActive) return;

        isActive = true;
        activeTimer = 0f;

        // 톱날 활성화
        foreach (var top in activeTops)
        {
            top.Reactivate();
        }

        // 활성화 이펙트
        CreateActivationEffect();

        // 활성화 사운드
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
    }

    // 가디언 비활성화
    public void DeactivateGuardian()
    {
        if (!isActive) return;

        isActive = false;

        // 톱날 비활성화
        foreach (var top in activeTops)
        {
            top.Deactivate();
        }

        // 진화 전이면 재소환 코루틴 시작
        if (!isEvolved)
        {
            if (activationCoroutine != null)
            {
                StopCoroutine(activationCoroutine);
            }
            activationCoroutine = StartCoroutine(ReactivateAfterDelay());
        }
    }

    // 지연 후 재활성화 코루틴
    private IEnumerator ReactivateAfterDelay()
    {
        yield return new WaitForSeconds(reactivateDelay);
        ActivateGuardian();
        activationCoroutine = null;
    }

    // 가디언 레벨업
    public void UpgradeGuardian()
    {
        if (currentLevel >= maxLevel) return;

        currentLevel++;
        currentLevel = Mathf.Min(currentLevel, maxLevel);

        // 스탯 업데이트
        UpdateLevelStats();

        // 레벨업 이펙트
        StartCoroutine(ShowLevelUpEffect());

        // 레벨업 사운드
        if (audioSource != null && levelUpSound != null)
        {
            audioSource.PlayOneShot(levelUpSound);
        }

        // 기존 톱날 파괴 후 재생성
        InitializeTops();

        Debug.Log($"Guardian 레벨업: Lv.{currentLevel}");
    }

    // 레벨업 이펙트 코루틴
    private IEnumerator ShowLevelUpEffect()
    {
        // 이펙트 생성
        if (levelUpEffect != null && playerTransform != null)
        {
            GameObject effect = Instantiate(levelUpEffect, playerTransform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 2f;
            Destroy(effect, UPGRADE_EFFECT_DURATION);
        }

        // 톱날 깜빡임 효과
        foreach (var top in activeTops)
        {
            StartCoroutine(FlashTop(top));
        }

        yield return new WaitForSeconds(UPGRADE_EFFECT_DURATION);
    }

    // 톱날 깜빡임 효과
    private IEnumerator FlashTop(GuardianTop top)
    {
        SpriteRenderer renderer = top.GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;

        Color originalColor = renderer.color;

        for (int i = 0; i < 3; i++)
        {
            renderer.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            renderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    // 진화 (Defender로)
    public void EvolveGuardian()
    {
        isEvolved = true;

        // 최고 레벨로 설정
        currentLevel = maxLevel;
        UpdateLevelStats();

        // 영구 활성화
        if (!isActive)
        {
            ActivateGuardian();
        }

        // 진화 이펙트
        CreateEvolutionEffect();

        Debug.Log("Guardian가 Defender로 진화했습니다!");
    }

    // 활성화 이펙트
    private void CreateActivationEffect()
    {
        if (activationEffect == null || playerTransform == null) return;

        GameObject effect = Instantiate(activationEffect, playerTransform.position, Quaternion.identity);
        Destroy(effect, 2f);
    }

    // 진화 이펙트
    private void CreateEvolutionEffect()
    {
        // 진화 특수 이펙트 생성
        if (Resources.Load("Effects/EvolutionEffect") != null && playerTransform != null)
        {
            GameObject effect = Instantiate(Resources.Load("Effects/EvolutionEffect") as GameObject);
            effect.transform.position = playerTransform.position;
            Destroy(effect, 3f);
        }
    }

    // 모든 톱날 정리
    private void ClearAllTops()
    {
        foreach (var top in activeTops)
        {
            if (top != null)
            {
                top.Deactivate();
                ObjectPoolManager.Instance.ReturnGuardianTop(top);
            }
        }
        activeTops.Clear();
    }

    // 현재 상태 정보 반환
    public GuardianSkillData GetCurrentData()
    {
        return new GuardianSkillData
        {
            level = currentLevel,
            topCount = currentTopCount,
            rotationSpeed = currentRotationSpeed,
            damageMultiplier = currentDamageMultiplier,
            duration = currentDuration,
            isActive = isActive,
            remainingTime = isEvolved ? -1f : currentDuration - activeTimer
        };
    }

    // 외부에서 현재 레벨 설정용
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, maxLevel);
        InitializeLevel();
    }

    // 활성 상태 확인
    public bool IsGuardianActive()
    {
        return isActive;
    }

    // 남은 시간 확인
    public float GetRemainingTime()
    {
        if (isEvolved) return -1f; // 진화 시 무한
        return Mathf.Max(0f, currentDuration - activeTimer);
    }

    private void OnDestroy()
    {
        // 코루틴 정리
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
        }

        // 톱날 정리
        ClearAllTops();
    }
}