using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoosAttackRay : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer; // 감지할 레이어

    private BossController boss;   // 뇌(BossController) 참조
    private Vector2 currentDir;    // 매 프레임 갱신되는 방향

    // ----------------------
    // 초기화: 보스 연결
    // ----------------------
    public void Initialize(BossController controller)
    {
        boss = controller;
    }

    // ----------------------
    // 예고 단계에서 호출: 플레이어 위치 추적
    // 매 프레임 갱신
    // ----------------------
    public void UpdateDirection()
    {
        if (boss == null || boss.target == null) return;

        // 눈이 보는 방향 갱신
        currentDir = (boss.target.position - (Vector3)transform.position).normalized;
    }

    // ----------------------
    // 공격 발동 시 호출
    // 레이 반환, 길이와 방향만 사용
    // 충돌 여부 판단은 패턴에서 처리
    // ----------------------
    public RaycastHit2D Fire()
    {
        if (boss == null) return default;

        return Physics2D.Raycast(
            transform.position,
            currentDir,              // 이미 UpdateDirection에서 계산된 방향 사용
            boss.CurrentAttackRayLength,
            targetLayer
        );
    }

    // ----------------------
    // 현재 보는 방향 반환 (패턴에서 참조 가능)
    // ----------------------
    public Vector2 CurrentDirection => currentDir;

}
