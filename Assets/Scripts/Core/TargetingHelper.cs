using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 타겟팅 로직 중복 제거를 위한 정적 헬퍼 클래스 (가장 가까운 적 찾기, 반경 내 모든 적 찾기)
/// Enemy와 Boss 모두 지원 (IDamageable 인터페이스)
/// </summary>
public static class TargetingHelper
{
    private const int BUFFER_SIZE = 256;
    private static Collider2D[] _buffer = new Collider2D[BUFFER_SIZE];

    /// <summary>
    /// 반경 내의 가장 가까운 대상을 탐색 (IDamageable - Enemy, Boss 모두 지원)
    /// SqrMagnitude를 사용하여 루트 연산을 제거해 성능 최적화
    /// </summary>
    public static IDamageable FindNearestDamageable(Vector3 origin, float radius, int layerMask)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, radius, _buffer, layerMask);
        if (hitCount == 0) return null;

        IDamageable nearest = null;
        float minDistanceSq = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (_buffer[i] == null) continue;
            if (!_buffer[i].TryGetComponent<IDamageable>(out var damageable)) continue;
            if (damageable == null || !damageable.GetType().IsSubclassOf(typeof(MonoBehaviour)) ||
                !(damageable as MonoBehaviour).gameObject.activeInHierarchy || damageable.CurrentHP <= 0) continue;

            float distSq = Vector3.SqrMagnitude(origin - (_buffer[i].transform.position));
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                nearest = damageable;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 반경 내의 가장 가까운 적을 탐색 (Enemy 전용 - 하위 호환성)
    /// </summary>
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
    /// 반경 내의 모든 대상을 탐색 (IDamageable - Enemy, Boss 모두 지원)
    /// In-place 패턴으로 메모리 할당을 최소화
    /// </summary>
    public static int FindAllDamageables(Vector3 origin, float radius, int layerMask, List<IDamageable> results)
    {
        if (results == null) return 0;

        results.Clear();
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, radius, _buffer, layerMask);
        if (hitCount == 0) return 0;

        for (int i = 0; i < hitCount; i++)
        {
            if (_buffer[i] == null) continue;
            if (!_buffer[i].TryGetComponent<IDamageable>(out var damageable)) continue;
            if (damageable == null || !damageable.GetType().IsSubclassOf(typeof(MonoBehaviour)) ||
                !(damageable as MonoBehaviour).gameObject.activeInHierarchy || damageable.CurrentHP <= 0) continue;

            results.Add(damageable);
        }

        return results.Count;
    }

    /// <summary>
    /// 반경 내의 모든 적을 탐색 (Enemy 전용 - 하위 호환성)
    /// In-place 패턴으로 메모리 할당을 최소화
    /// </summary>
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
