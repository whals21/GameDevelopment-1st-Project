using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class RPGExplosion : MonoBehaviour
{
    #region Serialized Fields
    [Header("폭발 설정")]
    [SerializeField] private float maxRadius = 2.5f;
    [SerializeField] private float growTime = 0.2f;
    [SerializeField] private float fadeTime = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("데미지 설정")]
    [SerializeField] private bool useFalloff = true; // 거리별 데미지 감소
    [SerializeField] private float minDamageMultiplier = 0.5f; // 가장자리 데미지 비율

    [Header("시각 효과")]
    [SerializeField] private Sprite[] explosionSprites; // 폭발 애니메이션 스프라이트
    [SerializeField] private float frameRate = 30f;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1, 0.5f, 0, 0);
    [SerializeField] private float finalScale = 2.0f; // 폭발 최종 크기 (1.0 = 원본 크기)
    [Range(0.5f, 5.0f)]
    [SerializeField] private float visualScaleMultiplier = 1.5f; // 시각적 크기 보정 (데미지 반경과는 별개)

    [Header("라이트 효과 (선택사항)")]
    [SerializeField] private bool useLight2D = false; // Light2D 사용 여부
    [SerializeField] private GameObject lightObject; // Light 2D 오브젝트
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float lightDuration = 0.3f;

    [Header("파티클 효과")]
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private ParticleSystem debrisParticles;

    [Header("오디오")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private float soundVolume = 1f;
    #endregion

    #region Private Fields
    private float damage;
    private float currentRadius;
    private SpriteRenderer spriteRenderer;
    private new Light2D light;
    private AudioSource audioSource;
    private bool isExploding = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 컴포넌트 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Light 2D 설정
        if (lightObject != null)
        {
            light = lightObject.GetComponent<Light2D>();
        }
        else
        {
            // 기본 Light 생성
            GameObject lightGO = new GameObject("ExplosionLight");
            lightGO.transform.SetParent(transform);
            light = lightGO.AddComponent<Light2D>();

            // URP 버전별 호환성 처리
            #if UNITY_2020_2_OR_NEWER
                // Unity 2020.2 이상 URP
                if (light.GetType().GetProperty("lightType") != null)
                {
                    light.GetType().GetProperty("lightType")?.SetValue(light, 0); // Point
                }
            #else
                // 이전 버전
                light.GetType().GetProperty("type")?.SetValue(light, 0);
            #endif

            lightObject = lightGO;
        }

        // 필요한 컴포넌트 추가
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "Effects";
            spriteRenderer.sortingOrder = 200;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // 기본값 설정
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        // 상태 초기화
        isExploding = false;
        currentRadius = 0f;

        // 이펙트 중지
        if (explosionParticles != null)
        {
            explosionParticles.Stop();
        }

        if (debrisParticles != null)
        {
            debrisParticles.Stop();
        }

        // 광원 끄기
        if (light != null)
        {
            light.intensity = 0f;
        }

        // 투명도 초기화
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 폭발 설정 및 실행
    /// </summary>
    public void SetExplosion(Vector3 position, float damage, float radius, LayerMask enemyLayer)
    {
        this.damage = damage;
        this.maxRadius = radius;
        this.enemyLayer = enemyLayer;

        // 위치 설정
        transform.position = position;
        transform.localScale = Vector3.one * 0.1f; // 시작은 작게

        // 활성화 및 폭발 실행
        gameObject.SetActive(true);
        StartCoroutine(ExplosionSequence());
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 폭발 시퀀스
    /// </summary>
    private IEnumerator ExplosionSequence()
    {
        if (isExploding) yield break;

        isExploding = true;

        // 즉시 데미지 적용
        ApplyExplosionDamage();

        // 사운드 재생
        PlayExplosionSound();

        // 파티클 재생
        PlayParticles();

        // 성장 애니메이션
        yield return StartCoroutine(GrowAnimation());

        // 페이드 아웃 애니메이션
        yield return StartCoroutine(FadeOutAnimation());

        // 풀에 반환
        ReturnToPool();
    }

    /// <summary>
    /// 폭발 데미지 적용
    /// </summary>
    private void ApplyExplosionDamage()
    {
        // 반경 내 모든 적 찾기
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, maxRadius, enemyLayer);

        int hitCount = 0;
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (enemyCollider.TryGetComponent<Enemy>(out Enemy enemy))
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                float damageMultiplier = 1f;

                // 거리별 데미지 감쇠
                if (useFalloff)
                {
                    float normalizedDistance = Mathf.Clamp01(distance / maxRadius);
                    damageMultiplier = Mathf.Lerp(1f, minDamageMultiplier, normalizedDistance);
                }

                float finalDamage = damage * damageMultiplier;
                enemy.TakeDamage(finalDamage);
                hitCount++;

                Debug.Log($"RPGExplosion: 적 {enemy.name}에게 {finalDamage:F1} 데미지 (거리: {distance:F2}m, 배수: {damageMultiplier:F2})");
            }
        }

        Debug.Log($"RPGExplosion: 총 {hitCount}마리 적에게 데미지 적용");
    }

    /// <summary>
    /// 성장 애니메이션
    /// </summary>
    private IEnumerator GrowAnimation()
    {
        float timer = 0f;
        Vector3 startScale = Vector3.one * 0.1f;
        // 데미지 반경(maxRadius)에 시각적 배수를 곱해 최종 스케일 결정
        Vector3 targetScale = Vector3.one * (maxRadius * visualScaleMultiplier);

        // 스프라이트 애니메이션 시작
        if (explosionSprites != null && explosionSprites.Length > 0)
        {
            StartCoroutine(SpriteAnimation());
        }

        while (timer < growTime)
        {
            timer += Time.deltaTime;
            float progress = timer / growTime;

            // 스케일 성장
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

            // 광원 효과
            if (light != null)
            {
                light.intensity = Mathf.Lerp(0f, lightIntensity, progress);
                // 광원 반경은 폭발 반경에 비례하지만 최대값 제한
                float lightRadius = Mathf.Min(maxRadius * 0.5f, 3f);

                // URP 버전별 호환성 처리
                #if UNITY_2020_2_OR_NEWER
                    if (light.GetType().GetProperty("pointLightOuterRadius") != null)
                    {
                        light.GetType().GetProperty("pointLightOuterRadius")?.SetValue(light, lightRadius);
                    }
                #else
                    if (light.GetType().GetProperty("falloffIntensity") != null)
                    {
                        light.GetType().GetProperty("falloffIntensity")?.SetValue(light, 0.5f);
                    }
                #endif
            }

            yield return null;
        }

        transform.localScale = targetScale;
    }

    /// <summary>
    /// 스프라이트 애니메이션
    /// </summary>
    private IEnumerator SpriteAnimation()
    {
        if (explosionSprites == null || explosionSprites.Length == 0)
            yield break;

        float frameDuration = 1f / frameRate;
        int currentFrame = 0;

        // 초기 스프라이트 설정
        spriteRenderer.sprite = explosionSprites[0];
        spriteRenderer.color = startColor;

        while (currentFrame < explosionSprites.Length)
        {
            spriteRenderer.sprite = explosionSprites[currentFrame];
            currentFrame++;
            yield return new WaitForSeconds(frameDuration);
        }
    }

    /// <summary>
    /// 페이드 아웃 애니메이션
    /// </summary>
    private IEnumerator FadeOutAnimation()
    {
        float timer = 0f;
        Color currentColor = spriteRenderer.color;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeTime;

            // 투명도 감소
            Color newColor = Color.Lerp(startColor, endColor, progress);
            spriteRenderer.color = newColor;

            // 광원 감소
            if (light != null)
            {
                light.intensity = Mathf.Lerp(lightIntensity, 0f, progress);
            }

            // 스케일 약간 증가 (최대 시각적 크기를 넘지 않도록 제한)
            Vector3 currentScale = transform.localScale;
            Vector3 maxAllowedScale = Vector3.one * (maxRadius * visualScaleMultiplier);
            currentScale = Vector3.Lerp(currentScale, maxAllowedScale, progress * 0.3f);
            transform.localScale = currentScale;

            yield return null;
        }

        // 최종 상태
        spriteRenderer.color = endColor;
        if (light != null)
        {
            light.intensity = 0f;
        }
    }

    /// <summary>
    /// 폭발 사운드 재생
    /// </summary>
    private void PlayExplosionSound()
    {
        if (explosionSound != null && audioSource != null)
        {
            audioSource.clip = explosionSound;
            audioSource.volume = soundVolume;
            audioSource.pitch = Random.Range(0.9f, 1.1f); // 약간의 피치 변화
            audioSource.Play();
        }
    }

    /// <summary>
    /// 파티클 재생
    /// </summary>
    private void PlayParticles()
    {
        // 메인 폭발 파티클
        if (explosionParticles != null)
        {
            explosionParticles.transform.position = transform.position;
            explosionParticles.Play();
        }

        // 파편 파티클
        if (debrisParticles != null)
        {
            debrisParticles.transform.position = transform.position;
            debrisParticles.Play();
        }
    }

    /// <summary>
    /// 풀에 반환
    /// </summary>
    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnRPGExplosion(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region Debug
    private void OnDrawGizmos()
    {
        // 폭발 반경 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxRadius);

        // 시각적 크기 표시 (데미지 반경과 시각적 배수로 계산)
        float visualRadius = maxRadius * visualScaleMultiplier;
        if (visualRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, visualRadius);
        }
    }

    private void OnValidate()
    {
        // 값 보정
        maxRadius = Mathf.Max(0.5f, maxRadius);
        growTime = Mathf.Max(0.05f, growTime);
        fadeTime = Mathf.Max(0.1f, fadeTime);
        minDamageMultiplier = Mathf.Clamp01(minDamageMultiplier);
    }
    #endregion
}