using UnityEngine;
using System.Collections;

/// <summary>
/// 번개 스프라이트 애니메이션, 사운드, 광원 효과를 재생하는 VFX 컴포넌트
/// </summary>
public class LightningStrike : MonoBehaviour
{
    #region Serialized Fields
    [Header("Visuals - 시각 효과 설정만")]
    [SerializeField] private Sprite[] lightningSprites;
    [SerializeField] private float frameRate = 30f;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Light Effect")]
    [SerializeField] private Light lightningLight;
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float lightFlickerSpeed = 15f;

    [Header("Sound")]
    [SerializeField] private AudioClip strikeSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.7f;
    #endregion

    #region Private Fields
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private Vector3 _originalScale;
    #endregion

    #region Initialization
    private void Awake()
    {
        // 컴포넌트 캐싱
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();

        // AudioSource 설정
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.volume = soundVolume;

        // 원본 스케일 저장
        _originalScale = transform.localScale;

        // 초기 상태
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }

        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
        }
    }

    private void OnEnable()
    {
        // 활성화 즉시 애니메이션 시작
        StartCoroutine(PlayEffectSequence());
    }

    /// <summary>
    /// 비활성화 시 자동 리셋 (캡슐화 - 전문가 피드백)
    /// ObjectPool.Return()에서 SetActive(false) 호출 시 자동으로 실행됨
    /// </summary>
    private void OnDisable()
    {
        ResetForReuse();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 위치 설정 (전문가 피드백: 데미지 제거, 위치만 전달)
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        position.z = 0f; // 2D 공간으로 고정
        transform.position = position;
    }

    /// <summary>
    /// 풀링 재사용을 위한 리셋
    /// </summary>
    public void ResetForReuse()
    {
        StopAllCoroutines();

        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
            _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
            _spriteRenderer.sprite = null;
        }

        transform.localScale = _originalScale;

        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
        }
    }
    #endregion

    #region Effect Sequence
    /// <summary>
    /// 번개 이펙트 시퀀스 (애니메이션 + 소리 + 빛)
    /// </summary>
    private IEnumerator PlayEffectSequence()
    {
        if (lightningSprites == null || lightningSprites.Length == 0)
        {
            ReturnToPool();
            yield break;
        }

        // 1. 사운드 재생
        if (strikeSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(strikeSound);
        }

        // 2. 스프라이트 애니메이션
        float totalDuration = (float)lightningSprites.Length / frameRate;
        float timer = 0f;

        while (timer < totalDuration)
        {
            // 프레임 업데이트
            int frameIndex = Mathf.FloorToInt(timer * frameRate);
            if (frameIndex < lightningSprites.Length && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = lightningSprites[frameIndex];
            }

            // 광원 깜빡임 효과
            if (lightningLight != null)
            {
                float flicker = Mathf.Sin(Time.time * lightFlickerSpeed) * 0.3f + 0.7f;
                lightningLight.intensity = lightIntensity * flicker;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. 페이드 아웃
        yield return StartCoroutine(FadeOut());

        // 4. 풀 반납
        ReturnToPool();
    }

    /// <summary>
    /// 페이드 아웃 효과
    /// </summary>
    private IEnumerator FadeOut()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            if (_spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                _spriteRenderer.color = new Color(endColor.r, endColor.g, endColor.b, alpha);
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
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = new Color(endColor.r, endColor.g, endColor.b, 0f);
        }

        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
        }
    }

    /// <summary>
    /// 풀 반납
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

    #region Editor Helper
    [ContextMenu("Preview Effect")]
    private void PreviewEffect()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(PlayEffectSequence());
        }
    }
    #endregion
}
