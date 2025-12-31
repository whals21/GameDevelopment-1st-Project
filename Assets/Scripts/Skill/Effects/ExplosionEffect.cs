using UnityEngine;
using System.Collections;

/// <summary>
/// 폭발 스프라이트 애니메이션과 사운드를 재생하는 VFX 컴포넌트
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    #region Serialized Fields
    [Header("Visuals - 시각 효과 설정")]
    [SerializeField] private Sprite[] explosionSprites;
    [SerializeField] private float frameRate = 30f;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Sound")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.8f;

    [Header("Scale")]
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 1.5f;
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
    }

    private void OnEnable()
    {
        // 활성화 즉시 애니메이션 시작
        StartCoroutine(PlayEffectSequence());
    }

    /// <summary>
    /// 비활성화 시 자동 리셋
    /// ObjectPool.Return()에서 SetActive(false) 호출 시 자동으로 실행됨
    /// </summary>
    private void OnDisable()
    {
        ResetForReuse();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 위치 설정
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        position.z = 0f; // 2D 공간으로 고정
        transform.position = position;
    }

    /// <summary>
    /// 크기 설정 (선택적)
    /// </summary>
    public void SetScale(float scale)
    {
        transform.localScale = Vector3.one * scale;
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
    }
    #endregion

    #region Effect Sequence
    /// <summary>
    /// 폭발 이펙트 시퀀스 (애니메이션 + 소리 + 스케일)
    /// </summary>
    private IEnumerator PlayEffectSequence()
    {
        // 스프라이트가 없으면 바로 반환
        if (explosionSprites == null || explosionSprites.Length == 0)
        {
            ReturnToPool();
            yield break;
        }

        // 1. 초기화
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = startColor;
        }
        transform.localScale = Vector3.one * startScale;

        // 2. 사운드 재생
        if (explosionSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(explosionSound);
        }

        // 3. 스프라이트 애니메이션
        float totalDuration = (float)explosionSprites.Length / frameRate;
        float timer = 0f;

        while (timer < totalDuration)
        {
            // 프레임 업데이트
            int frameIndex = Mathf.FloorToInt(timer * frameRate);
            if (frameIndex < explosionSprites.Length && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = explosionSprites[frameIndex];
            }

            // 스케일 증가 (폭발 효과)
            float scaleProgress = timer / totalDuration;
            float currentScale = Mathf.Lerp(startScale, endScale, scaleProgress);
            transform.localScale = Vector3.one * currentScale;

            timer += Time.deltaTime;
            yield return null;
        }

        // 4. 페이드 아웃
        yield return StartCoroutine(FadeOut());

        // 5. 풀 반납
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

            timer += Time.deltaTime;
            yield return null;
        }

        // 최종 상태
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = new Color(endColor.r, endColor.g, endColor.b, 0f);
        }
    }

    /// <summary>
    /// 풀 반납
    /// </summary>
    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnExplosion(this);
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
