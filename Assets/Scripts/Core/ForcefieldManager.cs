using UnityEngine;

public class ForcefieldManager : MonoBehaviour
{
    public static ForcefieldManager Instance { get; private set; }

    [Header("보호막 설정")]
    [SerializeField] private GameObject forcefieldPrefab;
    [SerializeField] private Transform forcefieldParent;

    private Forcefield[] activeForcefields;
    private SkillData forcefieldSkill;
    private int forcefieldLevel = 0;
    private bool isForcefieldActive = false;

    // 레벨별 설정
    private readonly float[] baseRadius = { 3f, 3.5f, 3f, 3.5f, 4f };
    private readonly float[] outerRadius = { 0f, 0f, 4f, 4.5f, 5f };
    private readonly float[] damageMultipliers = { 1f, 1.5f, 2f, 2.25f, 2.5f };

    private void Awake()
    {
        SetupSingleton();
    }

    private void Update()
    {
        // 플레이어 위치를 따라 보호막 위치 업데이트
        if (isForcefieldActive && activeForcefields != null)
        {
            UpdateForcefieldPositions();
        }
    }

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
    }

    //보호막 스킬 장착
    public void EquipForcefieldSkill(SkillData skill, int level)
    {
        forcefieldSkill = skill;
        forcefieldLevel = level;

        if (skill != null && level > 0)
        {
            ActivateForcefields();
        }
    }

    // 보호막 레벨 업데이트
    public void UpdateForcefieldLevel(int newLevel)
    {
        if (newLevel != forcefieldLevel && forcefieldSkill != null)
        {
            DeactivateForcefields();
            forcefieldLevel = newLevel;
         
            if(newLevel > 0)
            {
                ActivateForcefields();
            }
        }
    }   

    // 보호막 해제
    public void UnequipForcefield()
    {
        DeactivateForcefields();
        forcefieldSkill = null;
        forcefieldLevel = 0;
    }

    // 보호막 활성화
    private void ActivateForcefields()
    {
        if (forcefieldPrefab == null || forcefieldParent == null)
        {
            Debug.LogError("ForcefieldManager: Forcefield prefab or parent is not assigned.");
            return;
        }
        // 레벨에 따른 보호막 개수 결정
        int fieldCount = (forcefieldLevel >= 3) ? 2 : 1;
        activeForcefields = new Forcefield[fieldCount];

        // 보호막 생성
        for (int i = 0; i < fieldCount; i++)
        {
            GameObject fieldObj = Instantiate(forcefieldPrefab, forcefieldParent);
            activeForcefields[i] = fieldObj.GetComponent<Forcefield>();

            if (activeForcefields[i] == null)
            {
                Debug.LogError("ForcefieldManager: Forcefield component not found on prefab.");
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
    private void DeactivateForcefields()
    {
        if (activeForcefields != null)
        {
            foreach (var forcefield in activeForcefields)
            {
                if (forcefield != null)
                {
                    forcefield.Deactivate();
                    Destroy(forcefield.gameObject);
                }
            }
        }

        activeForcefields = null;
        isForcefieldActive = false;
    }


    // 보호막 위치 업데이트 (플레이어 위치에 맞춤)
    private void UpdateForcefieldPositions()
    {
        Vector3 playerPosition = GameManager.Instance.player.transform.position;

        foreach (var forcefield in activeForcefields)
        {
            if (forcefield != null)
            {
                forcefield.transform.position = playerPosition;
            }
        }
    }
}