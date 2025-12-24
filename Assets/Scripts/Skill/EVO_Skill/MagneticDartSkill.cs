using UnityEngine;

/// <summary>
/// 마그네틱 다트 진화 스킬 관리자
/// 2개의 투사체를 생성하고 관리하여 플레이어 주변을 공전하게 함
/// </summary>
public class MagneticDartSkill : MonoBehaviour
{
    #region Serialized Fields
    [Header("스킬 설정")]
    [SerializeField] private int baseDamage = 15;
    #endregion

    #region Private Fields
    private MagneticDartProjectile[] darts; // 2개 투사체
    private Transform player;
    private bool isActive = false;
    private int currentLevel = 1;
    #endregion

    #region Properties
    public bool IsActive => isActive;
    public int CurrentLevel => currentLevel;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 플레이어 찾기
        FindPlayer();
    }

    private void OnDestroy()
    {
        Deactivate();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 스킬 활성화
    /// </summary>
    /// <param name="level">스킬 레벨</param>
    public void Activate(int level)
    {
        if (isActive) return;

        currentLevel = Mathf.Clamp(level, 1, 5);
        CreateDarts();
        isActive = true;
    }

    /// <summary>
    /// 스킬 비활성화
    /// </summary>
    public void Deactivate()
    {
        if (!isActive) return;

        DestroyDarts();
        isActive = false;
    }

    /// <summary>
    /// 레벨 업데이트
    /// </summary>
    /// <param name="newLevel">새 레벨</param>
    public void SetLevel(int newLevel)
    {
        if (newLevel == currentLevel) return;

        currentLevel = Mathf.Clamp(newLevel, 1, 5);

        // 이미 활성화되어 있으면 투사체 재생성
        if (isActive)
        {
            DestroyDarts();
            CreateDarts();
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 플레이어 Transform을 찾습니다
    /// </summary>
    private void FindPlayer()
    {
        if (player != null) return; // 이미 찾았으면 패스

        // GameManager에서 플레이어 찾기
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            player = GameManager.Instance.player.transform;
            return;
        }

        // 태그로 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            return;
        }

        // PlayerController 컴포넌트로 찾기
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
    }

    /// <summary>
    /// 투사체 생성
    /// </summary>
    private void CreateDarts()
    {
        // 플레이어가 없으면 다시 검색 시도
        if (player == null)
        {
            FindPlayer();
        }

        if (player == null)
        {
            Debug.LogError("[MagneticDartSkill] 플레이어를 찾을 수 없습니다!");
            return;
        }

        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("[MagneticDartSkill] ObjectPoolManager.Instance가 null입니다!");
            return;
        }

        darts = new MagneticDartProjectile[2];

        for (int i = 0; i < 2; i++)
        {
            MagneticDartProjectile dart = ObjectPoolManager.Instance.GetMagneticDart();
            if (dart != null)
            {
                dart.Initialize(player, i == 0); // i==0은 시계방향, i==1은 반대
                dart.SetLevel(currentLevel);
                darts[i] = dart;
            }
            else
            {
                Debug.LogError($"[MagneticDartSkill] MagneticDartProjectile을 풀에서 가져올 수 없습니다! 인덱스: {i}");
            }
        }
    }

    /// <summary>
    /// 투사체 파괴
    /// </summary>
    private void DestroyDarts()
    {
        if (darts != null)
        {
            if (ObjectPoolManager.Instance != null)
            {
                foreach (var dart in darts)
                {
                    if (dart != null)
                    {
                        dart.Deactivate();
                        ObjectPoolManager.Instance.ReturnMagneticDart(dart);
                    }
                }
            }
            else
            {
                // 풀이 없으면 직접 파괴
                foreach (var dart in darts)
                {
                    if (dart != null)
                    {
                        Destroy(dart.gameObject);
                    }
                }
            }
        }
        darts = null;
    }
    #endregion
}
