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
    [SerializeField] private bool useFalloff = true;
    [SerializeField] private float minDamageMultiplier = 0.5f;

    [Header("시각 효과")]
    [SerializeField] private Sprite[] explosionSprites;
    [SerializeField] private float frameRate = 30f;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1, 0.5f, 0, 0);
    [Range(0.5f, 5.0f)]
    [SerializeField] private float visualScaleMultiplier = 1.5f;

    [Header("라이트 효과 (선택사항)")]
    [SerializeField] private GameObject lightObject;
    [SerializeField] private float lightIntensity = 3f;

    [Header("파티클 효과")]
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private ParticleSystem debrisParticles;

    [Header("오디오")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private float soundVolume = 1f;
    #endregion

    #region Private Fields
    private float damage;
    private SpriteRenderer spriteRenderer;
    private new Light2D light;
    private AudioSource audioSource;
    private bool isExploding = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (lightObject != null)
        {
            light = lightObject.GetComponent<Light2D>();
        }
        else
        {
            GameObject lightGO = new GameObject("ExplosionLight");
            lightGO.transform.SetParent(transform);
            light = lightGO.AddComponent<Light2D>();

            // URP 버전별 호환성 처리
            #if UNITY_2020_2_OR_NEWER
                if (light.GetType().GetProperty("lightType") != null)
                {
                    light.GetType().GetProperty("lightType")?.SetValue(light, 0);
                }
            #else
                light.GetType().GetProperty("type")?.SetValue(light, 0);
            #endif

            lightObject = lightGO;
        }

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

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        isExploding = false;

        if (explosionParticles != null)
        {
            explosionParticles.Stop();
        }

        if (debrisParticles != null)
        {
            debrisParticles.Stop();
        }

        if (light != null)
        {
            light.intensity = 0f;
        }

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }
    }
    #endregion

    #region Public Methods
    public void SetExplosion(Vector3 position, float damage, float radius, LayerMask enemyLayer)
    {
        this.damage = damage;
        this.maxRadius = radius;
        this.enemyLayer = enemyLayer;

        transform.position = position;
        transform.localScale = Vector3.one * 0.1f;

        gameObject.SetActive(true);
        StartCoroutine(ExplosionSequence());
    }
    #endregion

    #region Private Methods
    private IEnumerator ExplosionSequence()
    {
        if (isExploding) yield break;

        isExploding = true;

        ApplyExplosionDamage();
        PlayExplosionSound();
        PlayParticles();

        yield return StartCoroutine(GrowAnimation());

        yield return StartCoroutine(FadeOutAnimation());

        ReturnToPool();
    }

    private void ApplyExplosionDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, maxRadius, enemyLayer);

        int hitCount = 0;
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (enemyCollider.TryGetComponent<Enemy>(out Enemy enemy))
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                float damageMultiplier = 1f;

                if (useFalloff)
                {
                    float normalizedDistance = Mathf.Clamp01(distance / maxRadius);
                    damageMultiplier = Mathf.Lerp(1f, minDamageMultiplier, normalizedDistance);
                }

                float finalDamage = damage * damageMultiplier;
                enemy.TakeDamage(finalDamage);
                hitCount++;
            }
        }
    }

    private IEnumerator GrowAnimation()
    {
        float timer = 0f;
        Vector3 startScale = Vector3.one * 0.1f;
        Vector3 targetScale = Vector3.one * (maxRadius * visualScaleMultiplier);

        if (explosionSprites != null && explosionSprites.Length > 0)
        {
            StartCoroutine(SpriteAnimation());
        }

        while (timer < growTime)
        {
            timer += Time.deltaTime;
            float progress = timer / growTime;

            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

            if (light != null)
            {
                light.intensity = Mathf.Lerp(0f, lightIntensity, progress);
                float lightRadius = Mathf.Min(maxRadius * 0.5f, 3f);

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

    private IEnumerator SpriteAnimation()
    {
        if (explosionSprites == null || explosionSprites.Length == 0)
            yield break;

        float frameDuration = 1f / frameRate;
        int currentFrame = 0;

        spriteRenderer.sprite = explosionSprites[0];
        spriteRenderer.color = startColor;

        while (currentFrame < explosionSprites.Length)
        {
            spriteRenderer.sprite = explosionSprites[currentFrame];
            currentFrame++;
            yield return new WaitForSeconds(frameDuration);
        }
    }

    private IEnumerator FadeOutAnimation()
    {
        float timer = 0f;
        Color currentColor = spriteRenderer.color;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeTime;

            Color newColor = Color.Lerp(startColor, endColor, progress);
            spriteRenderer.color = newColor;

            if (light != null)
            {
                light.intensity = Mathf.Lerp(lightIntensity, 0f, progress);
            }

            Vector3 currentScale = transform.localScale;
            Vector3 maxAllowedScale = Vector3.one * (maxRadius * visualScaleMultiplier);
            currentScale = Vector3.Lerp(currentScale, maxAllowedScale, progress * 0.3f);
            transform.localScale = currentScale;

            yield return null;
        }

        spriteRenderer.color = endColor;
        if (light != null)
        {
            light.intensity = 0f;
        }
    }

    private void PlayExplosionSound()
    {
        if (explosionSound != null && audioSource != null)
        {
            audioSource.clip = explosionSound;
            audioSource.volume = soundVolume;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.Play();
        }
    }

    private void PlayParticles()
    {
        if (explosionParticles != null)
        {
            explosionParticles.transform.position = transform.position;
            explosionParticles.Play();
        }

        if (debrisParticles != null)
        {
            debrisParticles.transform.position = transform.position;
            debrisParticles.Play();
        }
    }

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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxRadius);

        float visualRadius = maxRadius * visualScaleMultiplier;
        if (visualRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, visualRadius);
        }
    }

    private void OnValidate()
    {
        maxRadius = Mathf.Max(0.5f, maxRadius);
        growTime = Mathf.Max(0.05f, growTime);
        fadeTime = Mathf.Max(0.1f, fadeTime);
        minDamageMultiplier = Mathf.Clamp01(minDamageMultiplier);
    }
    #endregion
}