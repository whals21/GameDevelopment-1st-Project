using UnityEngine;
using System.Collections.Generic;

public class ObjectPool<T> where T : MonoBehaviour
{
    private T prefab;
    private Transform parent;
    private Queue<T> pool;
    private int initialSize;

    // 생성자
    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.initialSize = initialSize;
        this.parent = parent;
        this.pool = new Queue<T>();

        // 초기 풀 생성
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    // 새 오브젝트 생성
    private T CreateNewObject()
    {
        T obj = Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }

    // 풀에서 오브젝트 가져오기
    public T Get()
    {
        // 풀이 비어있으면 새로 생성
        if (pool.Count == 0)
        {
            CreateNewObject();
        }

        T obj = pool.Dequeue();
        obj.gameObject.SetActive(true);
        return obj;
    }

    // 풀에 오브젝트 반환
    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

    // 현재 풀 크기
    public int PoolSize => pool.Count;
}