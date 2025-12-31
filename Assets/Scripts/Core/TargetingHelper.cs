using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 타겟팅 로직 중복 제거를 위한 정적 헬퍼 클래스 (가장 가까운 적 찾기, 반경 내 모든 적 찾기)
/// </summary>
public static class TargetingHelper
{
    private const int BUFFER_SIZE = 256;
    private static Collider2D[] _buffer = new Collider2D[BUFFER_SIZE];

    /// <summary>
    /// 반경 내의 가장 가까운 적을 탐색
    /// SqrMagnitude를 사용하여 루트 연산을 제거해 성능 최적화
    /// </summary>
    /// <param name="origin">탐색 원점</param>
    /// <param name="radius">탐색 반경</param>
    /// <param name="layerMask">적 레이어 마스크</param>
    /// <returns>가장 가까운 적 (없으면 null)</returns>
    public static Enemy FindNearestEnemy(Vector3 origin, float radius, int layerMask)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, radius, _buffer, layerMask);
        if (hitCount == 0) return null;

        Enemy nearest = null;
        float minDistanceSq = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (_buffer[i] == null) continue;
            if (!_buffer[i].TryGetComponent<Enemy>(out var enemy)) continue;
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.CurrentHP <= 0) continue;

            float distSq = Vector3.SqrMagnitude(origin - enemy.transform.position);
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                nearest = enemy;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 반경 내의 모든 적을 탐색
    /// In-place 패턴으로 메모리 할당을 최소화
    /// </summary>
    /// <param name="origin">탐색 원점</param>
    /// <param name="radius">탐색 반경</param>
    /// <param name="layerMask">적 레이어 마스크</param>
    /// <param name="results">결과를 담을 리스트 (매 호출마다 Clear 후 재사용)</param>
    /// <returns>찾은 적의 수</returns>
    public static int FindAllEnemies(Vector3 origin, float radius, int layerMask, List<Enemy> results)
    {
        if (results == null) return 0;

        results.Clear();
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, radius, _buffer, layerMask);
        if (hitCount == 0) return 0;

        for (int i = 0; i < hitCount; i++)
        {
            if (_buffer[i] == null) continue;
            if (!_buffer[i].TryGetComponent<Enemy>(out var enemy)) continue;
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.CurrentHP <= 0) continue;

            results.Add(enemy);
        }

        return results.Count;
    }
}
