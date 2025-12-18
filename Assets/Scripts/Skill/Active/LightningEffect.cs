using UnityEngine;
using System.Collections;

public class LightningEffect : MonoBehaviour
{
    #region Serialized Fields
    [Header("스프라이트 애니메이션")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] lightningSprites;
    [SerializeField] private float frameRate = 30f;
    [SerializeField] private bool loop = false;

    [Header("비주얼 효과")]
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;

    [Header("광원 효과")]
    [SerializeField] private GameObject lightObject;
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float lightFlickerSpeed = 15f;
    #endregion

    #region Private Fields
    private bool isPlaying = false;
    private Light lightComponent;
    private Vector3 originalScale;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 컴포넌트 초기화
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 원본 스케일 저장
        originalScale = transform.localScale;

        // 광원 설정
        if (lightObject != null)
        {
            lightComponent = lightObject.GetComponent<Light>();
            if (lightComponent != null)
            {
                lightObject.SetActive(false);
            }
        }

        // 초기 상태 설정
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }
    }

    private void OnEnable()
    {
        // 초기화
        ResetAnimation();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 번개 타격 애니메이션 재생
    /// </summary>
    public void PlayStrikeAnimation()
    {
        if (lightningSprites == null || lightningSprites.Length == 0)
        {
            Debug.LogWarning("LightningEffect: 번개 스프라이트가 설정되지 않았습니다!");
            return;
        }

        // GameObject 활성화 확인
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        isPlaying = true;

        // 첫 프레임 설정
        if (spriteRenderer != null && lightningSprites.Length > 0)
        {
            spriteRenderer.sprite = lightningSprites[0];
            spriteRenderer.color = startColor;
        }

        // 광원 활성화
        if (lightComponent != null)
        {
            lightObject.SetActive(true);
            lightComponent.intensity = lightIntensity;
        }

        // 애니메이션 시작
        StartCoroutine(AnimationSequence());
    }

    /// <summary>
    /// 애니메이션 정지
    /// </summary>
    public void StopAnimation()
    {
        isPlaying = false;
        StopAllCoroutines();

        // 광원 비활성화
        if (lightObject != null)
        {
            lightObject.SetActive(false);
        }
    }

    /// <summary>
    /// 풀링을 위한 초기화
    /// </summary>
    public void ResetForReuse()
    {
        ResetAnimation();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 애니메이션 초기화
    /// </summary>
    private void ResetAnimation()
    {
        isPlaying = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }

        if (lightObject != null)
        {
            lightObject.SetActive(false);
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// 애니메이션 시퀀스
    /// </summary>
    private IEnumerator AnimationSequence()
    {
        float totalDuration = (float)lightningSprites.Length / frameRate;
        float timer = 0f;

        while (timer < totalDuration && isPlaying)
        {
            // 프레임 업데이트
            int frameIndex = Mathf.FloorToInt(timer * frameRate);
            if (frameIndex >= lightningSprites.Length)
            {
                if (loop)
                {
                    frameIndex = frameIndex % lightningSprites.Length;
                }
                else
                {
                    break;
                }
            }

            // 스프라이트 변경
            if (spriteRenderer != null && frameIndex < lightningSprites.Length)
            {
                spriteRenderer.sprite = lightningSprites[frameIndex];
            }

            // 광원 깜빡임 효과
            if (lightComponent != null)
            {
                float flicker = Mathf.Sin(Time.time * lightFlickerSpeed) * 0.3f + 0.7f;
                lightComponent.intensity = lightIntensity * flicker;
            }

            // 스케일 변화 (충격 효과)
            float scaleValue = Mathf.Lerp(minScale, maxScale, (float)frameIndex / lightningSprites.Length);
            transform.localScale = originalScale * scaleValue;

            timer += Time.deltaTime;
            yield return null;
        }

        // 페이드 아웃
        yield return StartCoroutine(FadeOut());
    }

    /// <summary>
    /// 페이드 아웃 효과
    /// </summary>
    private IEnumerator FadeOut()
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
            if (lightComponent != null)
            {
                lightComponent.intensity = Mathf.Lerp(lightIntensity, 0f, timer / fadeDuration);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 최종 상태
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(endColor.r, endColor.g, endColor.b, 0f);
        }

        if (lightObject != null)
        {
            lightObject.SetActive(false);
        }

        isPlaying = false;
    }
    #endregion

    #region Editor Helper
    /// <summary>
    /// 에디터에서 애니메이션 미리보기
    /// </summary>
    [ContextMenu("Preview Animation")]
    private void PreviewAnimation()
    {
        if (Application.isPlaying)
        {
            PlayStrikeAnimation();
        }
    }
    #endregion
}