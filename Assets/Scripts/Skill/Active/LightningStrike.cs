using UnityEngine;
using System.Collections;

public class LightningStrike : MonoBehaviour
{
    #region Serialized Fields
    [Header("번개 설정")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float effectDuration = 0.5f;
    [SerializeField] private float strikeDelay = 0.1f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float strikeWidth = 0.5f;

    [Header("애니메이션")]
    [SerializeField] private Sprite[] lightningSprites;
    [SerializeField] private float frameRate = 30f;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;

    [Header("광원 효과")]
    [SerializeField] private Light lightningLight;
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float lightFlickerSpeed = 15f;

    [Header("사운드")]
    [SerializeField] private AudioClip strikeSound;
    [SerializeField] private float soundVolume = 0.7f;
    #endregion

    #region Private Fields
    private Enemy targetEnemy;
    private bool hasStruck = false;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private bool isAnimating = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 컴포넌트 초기화
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        // 원본 스케일 저장
        originalScale = transform.localScale;

        // 초기 상태 설정
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }

        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
        }
    }

    private void OnEnable()
    {
        // 초기화
        hasStruck = false;
        isAnimating = false;

        // 약간의 지연 후 번개 타격
        StartCoroutine(StrikeSequence());
    }

    private void OnDisable()
    {
        // 클린업
        StopAllCoroutines();
        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 번개 목표 설정 및 발동
    /// </summary>
    /// <param name="target">목표 적</param>
    /// <param name="strikeDamage">데미지</param>
    public void SetTarget(Enemy target, float strikeDamage = -1f)
    {
        targetEnemy = target;
        if (strikeDamage > 0f)
        {
            damage = strikeDamage;
        }

        // 목표 위치로 번개 이동
        if (target != null)
        {
            Vector3 targetPosition = target.transform.position;
            targetPosition.z = 0f; // 2D 공간으로 고정
            transform.position = targetPosition;
        }
    }

    /// <summary>
    /// 특정 위치에 번개 발동
    /// </summary>
    /// <param name="position">번개 위치</param>
    /// <param name="strikeDamage">데미지</param>
    public void SetPosition(Vector3 position, float strikeDamage = -1f)
    {
        targetEnemy = null;
        if (strikeDamage > 0f)
        {
            damage = strikeDamage;
        }

        position.z = 0f; // 2D 공간으로 고정
        transform.position = position;
    }

    /// <summary>
    /// 풀링을 위한 초기화
    /// </summary>
    public void ResetForReuse()
    {
        hasStruck = false;
        targetEnemy = null;
        isAnimating = false;

        // 이펙트 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
            spriteRenderer.sprite = null;
        }

        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
        }

        transform.localScale = originalScale;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 번개 타격 시퀀스
    /// </summary>
    private IEnumerator StrikeSequence()
    {
        // 약간의 지연 후 타격
        yield return new WaitForSeconds(strikeDelay);

        // 번개 애니메이션 시작
        StartCoroutine(PlayLightningAnimation());

        // 사운드 재생
        if (strikeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(strikeSound);
        }

        // 데미지 처리
        DealDamage();

        // 이펙트 지속 후 반환
        yield return new WaitForSeconds(effectDuration);

        // 오브젝트 풀에 반환
        ReturnToPool();
    }

    /// <summary>
    /// 번개 애니메이션 재생
    /// </summary>
    private IEnumerator PlayLightningAnimation()
    {
        if (lightningSprites == null || lightningSprites.Length == 0)
        {
            Debug.LogWarning("LightningStrike: 번개 스프라이트가 설정되지 않았습니다!");
            yield break;
        }

        isAnimating = true;

        // 첫 프레임 설정
        if (spriteRenderer != null && lightningSprites.Length > 0)
        {
            spriteRenderer.sprite = lightningSprites[0];
            spriteRenderer.color = startColor;
        }

        // 광원 활성화
        if (lightningLight != null)
        {
            lightningLight.intensity = lightIntensity;
        }

        // 스프라이트 애니메이션
        float totalDuration = (float)lightningSprites.Length / frameRate;
        float timer = 0f;

        while (timer < totalDuration && isAnimating)
        {
            // 프레임 업데이트
            int frameIndex = Mathf.FloorToInt(timer * frameRate);
            if (frameIndex >= lightningSprites.Length)
            {
                break;
            }

            // 스프라이트 변경
            if (spriteRenderer != null && frameIndex < lightningSprites.Length)
            {
                spriteRenderer.sprite = lightningSprites[frameIndex];
            }

            // 광원 깜빡임 효과
            if (lightningLight != null)
            {
                float flicker = Mathf.Sin(Time.time * lightFlickerSpeed) * 0.3f + 0.7f;
                lightningLight.intensity = lightIntensity * flicker;
            }

            // 스케일 변화 (충격 효과)
            float scaleValue = Mathf.Lerp(minScale, maxScale, (float)frameIndex / lightningSprites.Length);
            transform.localScale = originalScale * scaleValue;

            timer += Time.deltaTime;
            yield return null;
        }

        // 페이드 아웃
        yield return StartCoroutine(FadeOutEffect());

        isAnimating = false;
    }

    /// <summary>
    /// 페이드 아웃 효과
    /// </summary>
    private IEnumerator FadeOutEffect()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                spriteRenderer.color = new Color(endColor.r, endColor.g, endColor.b, alpha);
            }

            // 광원 감소
            if (lightningLight != null)
            {
                lightningLight.intensity = Mathf.Lerp(lightIntensity, 0f, timer / fadeDuration);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 최종 상태
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(endColor.r, endColor.g, endColor.b, 0f);
        }

        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
        }
    }

    /// <summary>
    /// 데미지 처리
    /// </summary>
    private void DealDamage()
    {
        if (hasStruck) return;
        hasStruck = true;

        // 목표 적에게 직접 데미지
        if (targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
        {
            targetEnemy.TakeDamage(damage);
        }

        // 주변 AOE 데미지 (선택적)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, strikeWidth, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy != null && enemy.TryGetComponent<Enemy>(out Enemy enemyComponent))
            {
                if (enemyComponent != targetEnemy) // 목표 적은 위에서 이미 데미지 입음
                {
                    enemyComponent.TakeDamage(damage * 0.5f); // AOE는 50% 데미지
                }
            }
        }

        Debug.Log($"LightningStrike: {transform.position}에서 {damage} 데미지 전달");
    }

    /// <summary>
    /// 오브젝트 풀에 반환
    /// </summary>
    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnLightning(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        // AOE 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, strikeWidth);
    }
    #endregion
}