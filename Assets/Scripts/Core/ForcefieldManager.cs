using UnityEngine;

public class ForcefieldManager : MonoBehaviour
{
    #region Singleton
    public static ForcefieldManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("보호막 설정")]
    [SerializeField] private GameObject forcefieldPrefab;
    [SerializeField] private Transform forcefieldParent; // 보호막들의 부모 Transform
    #endregion

    #region Private Fields
    private Forcefield[] activeForcefields;
    private SkillData forcefieldSkill;
    private int forcefieldLevel = 0;
    private bool isForcefieldActive = false;

    // 레벨별 설정
    private readonly float[] baseRadius = { 1f, 1.5f, 1.5f, 1.75f, 2f };
    private readonly float[] outerRadius = { 0f, 0f, 2f, 2.25f, 2.5f };
    private readonly float[] damageMultipliers = { 1f, 1.5f, 2f, 2.25f, 2.5f };
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        SetupSingleton();
    }

    private void Update()
    {
        // 보호막은 이제 플레이어의 자식으로 설정되어 자동으로 따라다니므로 Update 호출 불필요
        // 필요한 경우 다른 로직을 여기에 추가할 수 있음
    }
    #endregion

    #region Initialization
    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 부모 Transform이 없으면 생성
        if (forcefieldParent == null)
        {
            forcefieldParent = new GameObject("Forcefields").transform;
            forcefieldParent.SetParent(transform);
        }
    }
    #endregion

    #region Public API
    // 보호막 스킬 장착
    public void EquipForcefield(SkillData skill, int level)
    {
        forcefieldSkill = skill;
        forcefieldLevel = level;

        if (skill != null && level > 0)
        {
            ActivateForcefield();
        }
        else
        {
            DeactivateForcefield();
        }
    }

    // 보호막 레벨 업데이트
    public void UpdateForcefieldLevel(int newLevel)
    {
        if (newLevel != forcefieldLevel && forcefieldSkill != null)
        {
            DeactivateForcefield();
            forcefieldLevel = newLevel;
            if (newLevel > 0)
            {
                ActivateForcefield();
            }
        }
    }

    // 보호막 해제
    public void UnequipForcefield()
    {
        DeactivateForcefield();
        forcefieldSkill = null;
        forcefieldLevel = 0;
    }
    #endregion

    #region Forcefield Management
    // 보호막 활성화
    private void ActivateForcefield()
    {
        if (forcefieldPrefab == null)
        {
            Debug.LogError("ForcefieldManager: Forcefield 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 레벨에 따른 보호막 개수 결정
        int fieldCount = (forcefieldLevel >= 3) ? 2 : 1;
        activeForcefields = new Forcefield[fieldCount];

        // 보호막 생성
        for (int i = 0; i < fieldCount; i++)
        {
            GameObject fieldObj = Instantiate(forcefieldPrefab);
            activeForcefields[i] = fieldObj.GetComponent<Forcefield>();

            if (activeForcefields[i] == null)
            {
                Debug.LogError("ForcefieldManager: Forcefield 컴포넌트를 찾을 수 없습니다!");
                continue;
            }

            // 플레이어의 자식으로 설정하여 자동으로 따라다니게 함
            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                fieldObj.transform.SetParent(GameManager.Instance.player.transform);
                fieldObj.transform.localPosition = Vector3.zero;
                Debug.Log($"Forcefield {i}를 플레이어의 자식으로 설정함");
            }
            else
            {
                // 플레이어가 없을 경우 ForcefieldParent를 사용
                fieldObj.transform.SetParent(forcefieldParent);
                Debug.LogWarning("ForcefieldManager: 플레이어를 찾을 수 없어 ForcefieldParent를 사용합니다.");
            }

            // 반경 설정
            float radius = (i == 0) ? baseRadius[forcefieldLevel - 1] : outerRadius[forcefieldLevel - 1];

            // 데미지 계산
            float baseDamage = forcefieldSkill.damage;
            float damageMultiplier = damageMultipliers[forcefieldLevel - 1];
            float damage = baseDamage * damageMultiplier;

            // 초기화
            activeForcefields[i].Init(damage, radius);
        }

        isForcefieldActive = true;
    }

    // 보호막 비활성화
    private void DeactivateForcefield()
    {
        if (activeForcefields != null)
        {
            foreach (var field in activeForcefields)
            {
                if (field != null)
                {
                    field.Deactivate();
                    Destroy(field.gameObject);
                }
            }
        }

        activeForcefields = null;
        isForcefieldActive = false;
    }

    // 보호막 위치 업데이트 (플레이어 따라다님)
    private void UpdateForcefieldPositions()
    {
        Vector3 playerPosition = GameManager.Instance != null ?
            GameManager.Instance.player.transform.position :
            Vector3.zero;

        foreach (var field in activeForcefields)
        {
            if (field != null)
            {
                // UpdatePosition 메서드를 통해 위치 및 LineRenderer 업데이트
                field.UpdatePosition(playerPosition);
            }
        }
    }
    #endregion

    #region Utility
    // 현재 보호막 데미지 가져오기
    public float GetCurrentDamage()
    {
        if (!isForcefieldActive || forcefieldSkill == null || forcefieldLevel <= 0)
            return 0f;

        return forcefieldSkill.damage * damageMultipliers[forcefieldLevel - 1];
    }

    // 현재 보호막 반경들 가져오기
    public Vector2 GetCurrentRadii()
    {
        if (!isForcefieldActive || forcefieldLevel <= 0)
            return Vector2.zero;

        if (forcefieldLevel >= 3)
        {
            return new Vector2(baseRadius[forcefieldLevel - 1], outerRadius[forcefieldLevel - 1]);
        }
        else
        {
            return new Vector2(baseRadius[forcefieldLevel - 1], 0f);
        }
    }
    #endregion
}