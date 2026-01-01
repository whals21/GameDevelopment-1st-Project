using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스킬 목록 관리, 스킬 장착/해제/레벨업, UpdateSkill() 호출을 담당하는 매니저
/// List와 Dictionary 하이브리드로 순회와 검색을 최적화
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    #region Serialized Fields
    [Header("테스트용 스킬")]
    [SerializeField] private SkillData[] testSkills;

    [Header("테스트용 스킬 레벨")]
    [SerializeField] private int[] testSkillLevels = new int[5];
    #endregion

    #region Private Fields
    // 순회용 List (Update 루프에서 효율적)
    private List<SkillBase> _skillList = new List<SkillBase>();

    // 검색용 Dictionary (O(1) 빠른 조회)
    private Dictionary<SkillData, SkillBase> _skillLookup = new Dictionary<SkillData, SkillBase>();

    private const int MAX_SKILLS = 6;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 테스트용 스킬 장착
        EquipTestSkills();
    }

    private void Update()
    {
        // List로 순회 (Dictionary.Values보다 빠름)
        for (int i = 0; i < _skillList.Count; i++)
        {
            SkillBase skill = _skillList[i];
            if (skill != null && skill.IsActive)
            {
                skill.UpdateSkill();
            }
        }
    }
    #endregion

    #region Public API - 스킬 장착/해제
    /// <summary>
    /// 새로운 스킬을 장착
    /// Factory를 통해 적절한 스킬 컴포넌트를 생성
    /// </summary>
    /// <param name="data">스킬 데이터</param>
    /// <param name="level">시작 레벨</param>
    /// <returns>장착된 스킬 (이미 있으면 기존 스킬 반환)</returns>
    public SkillBase AddSkill(SkillData data, int level = 1)
    {
        if (data == null)
        {
            Debug.LogError("[SkillManager] 스킬 데이터가 null입니다.");
            return null;
        }

        // 이미 있는 스킬이면 반환 (O(1))
        if (_skillLookup.TryGetValue(data, out var existingSkill))
        {
            Debug.LogWarning($"[SkillManager] {data.skillName} 스킬은 이미 장착되어 있습니다.");
            return existingSkill;
        }

        if (_skillList.Count >= MAX_SKILLS)
        {
            Debug.LogWarning("[SkillManager] 스킬 슬롯이 가득 찼습니다.");
            return null;
        }

        // Factory를 통해 스킬 생성
        SkillBase newSkill = SkillFactory.CreateSkill(gameObject, data, level);

        if (newSkill == null)
        {
            Debug.LogError($"[SkillManager] 스킬 생성 실패: {data.skillName}");
            return null;
        }

        // List와 Dictionary에 추가
        _skillList.Add(newSkill);
        _skillLookup[data] = newSkill;

        // HUD 업데이트 (Active 스킬이므로 skillType = 0)
        if (SkillHUD.Instance != null)
        {
            SkillHUD.Instance.UpdateSkillUI(data.skillName, data.icon, level, 0);
        }

        Debug.Log($"[SkillManager] {data.skillName} 스킬 장착 완료 (레벨: {level})");

        return newSkill;
    }

    /// <summary>
    /// 스킬을 제거
    /// </summary>
    /// <param name="skill">제거할 스킬</param>
    public void RemoveSkill(SkillBase skill)
    {
        if (skill == null) return;

        skill.Deactivate();
        _skillList.Remove(skill);

        if (skill.Data != null)
        {
            _skillLookup.Remove(skill.Data);
        }

        Destroy(skill);

        Debug.Log("[SkillManager] 스킬 제거 완료");
    }

    /// <summary>
    /// 특정 스킬 데이터를 가진 스킬을 제거 (O(1)).
    /// </summary>
    /// <param name="data">제거할 스킬 데이터</param>
    public void RemoveSkill(SkillData data)
    {
        if (data == null) return;

        if (_skillLookup.TryGetValue(data, out var skill))
        {
            RemoveSkill(skill);
        }
    }

    /// <summary>
    /// 모든 스킬을 제거
    /// </summary>
    public void ClearAllSkills()
    {
        for (int i = _skillList.Count - 1; i >= 0; i--)
        {
            RemoveSkill(_skillList[i]);
        }

        Debug.Log("[SkillManager] 모든 스킬 제거 완료");
    }
    #endregion

    #region Public API - 스킬 조회
    /// <summary>
    /// 현재 장착된 스킬 개수를 반환
    /// </summary>
    public int GetSkillCount()
    {
        return _skillList.Count;
    }

    /// <summary>
    /// 특정 스킬 데이터의 레벨을 반환 (O(1)).
    /// </summary>
    /// <param name="data">스킬 데이터</param>
    /// <returns>레벨 (없으면 0)</returns>
    public int GetSkillLevel(SkillData data)
    {
        if (data == null) return 0;

        if (_skillLookup.TryGetValue(data, out var skill))
        {
            return skill.CurrentLevel;
        }

        return 0;
    }

    /// <summary>
    /// 특정 타입의 스킬이 장착되어 있는지 확인
    /// </summary>
    /// <param name="skillType">스킬 타입</param>
    /// <returns>장착되어 있으면 true</returns>
    public bool HasSkillType(SkillType skillType)
    {
        for (int i = 0; i < _skillList.Count; i++)
        {
            if (_skillList[i]?.Data?.skillType == skillType)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 모든 활성 스킬을 반환
    /// </summary>
    public IReadOnlyList<SkillBase> GetAllSkills()
    {
        return _skillList.AsReadOnly();
    }
    #endregion

    #region Public API - 스킬 레벨업
    /// <summary>
    /// 스킬을 레벨업하거나 새로 장착 (O(1)).
    /// </summary>
    /// <param name="skillData">레벨업/장착할 스킬 데이터</param>
    public void UpgradeOrEquipSkill(SkillData skillData)
    {
        if (skillData == null) return;

        // O(1)로 기존 스킬 확인
        if (_skillLookup.TryGetValue(skillData, out var skill))
        {
            // 이미 있으면 레벨업 (최대 레벨 5 체크)
            int newLevel = Mathf.Min(skill.CurrentLevel + 1, 5);
            skill.SetLevel(newLevel);

            // HUD 업데이트 (Active 스킬이므로 skillType = 0)
            if (SkillHUD.Instance != null)
            {
                SkillHUD.Instance.UpdateSkillUI(skillData.skillName, skillData.icon, newLevel, 0);
            }

            Debug.Log($"[SkillManager] {skillData.skillName} 스킬 레벨업: {newLevel}");
        }
        else
        {
            // 없으면 새로 장착
            AddSkill(skillData, 1);
        }
    }

    /// <summary>
    /// 스킬을 교체 (스킬진화 시 사용).
    /// </summary>
    /// <param name="oldSkill">제거할 기존 스킬</param>
    /// <param name="newSkill">장착할 새 스킬</param>
    /// <param name="level">새 스킬의 레벨</param>
    public void ReplaceSkill(SkillData oldSkill, SkillData newSkill, int level)
    {
        if (oldSkill == null || newSkill == null) return;

        // 기존 스킬 제거
        RemoveSkill(oldSkill);

        // 새 스킬 장착
        AddSkill(newSkill, level);

        Debug.Log($"[SkillManager] 스킬 교체: {oldSkill.skillName} → {newSkill.skillName}");
    }
    #endregion

    #region Test Functions
    /// <summary>
    /// 테스트용 스킬 장착
    /// </summary>
    private void EquipTestSkills()
    {
        if (testSkills == null) return;

        for (int i = 0; i < Mathf.Min(testSkills.Length, MAX_SKILLS); i++)
        {
            if (testSkills[i] != null)
            {
                int level = (i < testSkillLevels.Length) ? Mathf.Max(1, testSkillLevels[i]) : 1;
                AddSkill(testSkills[i], level);
            }
        }
    }

    /// <summary>
    /// 테스트용 스킬 레벨 설정 (인스펙터에서 호출)
    /// </summary>
    [ContextMenu("테스트 스킬 레벨 적용")]
    public void ApplyTestSkillLevels()
    {
        if (testSkills == null || testSkillLevels == null) return;

        for (int i = 0; i < Mathf.Min(testSkills.Length, testSkillLevels.Length); i++)
        {
            if (testSkills[i] != null && testSkillLevels[i] > 0)
            {
                int level = Mathf.Clamp(testSkillLevels[i], 1, 5);

                // Dictionary로 빠르게 조회
                if (_skillLookup.TryGetValue(testSkills[i], out var skill))
                {
                    skill.SetLevel(level);
                }
            }
        }
    }
    #endregion
}
