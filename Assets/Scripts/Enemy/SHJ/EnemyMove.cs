using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    private EnemyBase main;
    
    private void Awake()
    {
        main = GetComponent<EnemyBase>();
       
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    private void Move()
    {
        

        if (main.Target == null)
        {
            main.rb.velocity = Vector2.zero;
            return;
        }

        Vector2 direction = (main.Target.position - transform.position).normalized;

        // 이동
        main.rb.velocity = direction * main.Data.moveSpeed;

        // 좌우 반전
        if (direction.x > 0.01f)
            main.sr.flipX = false;
        else if (direction.x < -0.01f)
            main.sr.flipX = true;

    }
}
