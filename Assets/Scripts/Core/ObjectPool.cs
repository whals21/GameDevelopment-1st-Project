using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MonoBehaviour 오브젝트를 위한 제네릭 오브젝트 풀 (풀에서 가져오기, 반환, 크기 관리)
/// </summary>
/// <typeparam name="T">MonoBehaviour를 상속받은 타입</typeparam>
public class ObjectPool<T> where T : MonoBehaviour
{
    private T _prefab;
    private Transform _parent;
    private Queue<T> _pool;
    private int _initialSize;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="prefab">풀링할 프리팹</param>
    /// <param name="initialSize">초기 풀 크기</param>
    /// <param name="parent">부모 트랜스폼 (선택사항)</param>
    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _initialSize = initialSize;
        _parent = parent;
        _pool = new Queue<T>();

        // 초기 풀 생성
        for (int i = 0; i < _initialSize; i++)
        {
            CreateNewObject();
        }
    }

    /// <summary>
    /// 새 오브젝트 생성 (내부 사용)
    /// </summary>
    private T CreateNewObject()
    {
        T obj = Object.Instantiate(_prefab, _parent);
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
        return obj;
    }

    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// 풀이 비어있으면 자동으로 새로 생성
    /// </summary>
    public T Get()
    {
        // 풀이 비어있으면 새로 생성
        if (_pool.Count == 0)
        {
            CreateNewObject();
        }

        T obj = _pool.Dequeue();
        obj.gameObject.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 풀에 오브젝트 반환
    /// 비활성화하여 OnDisable 라이프사이클 트리거
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null) return;

        obj.gameObject.SetActive(false); // OnDisable 자동 호출 -> ResetForReuse()
        _pool.Enqueue(obj);
    }

    /// <summary>
    /// 현재 풀 크기 (디버깅용)
    /// </summary>
    public int PoolSize => _pool.Count;
}