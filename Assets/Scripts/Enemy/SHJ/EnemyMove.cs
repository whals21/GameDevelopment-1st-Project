using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    private Enemy main;
    private EnemyShooter shooter;
    private void Awake()
    {
        main = GetComponent<Enemy>();
        shooter = GetComponent<EnemyShooter>();
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

        float distance = Vector2.Distance(transform.position, main.Target.position);

        // 공격 범위 안에 있고, 원거리 에너미라면 → 멈춤
        if (main.IsRange && distance <= main.StopDistance)
        {
            main.rb.velocity = Vector2.zero;
            shooter.TryShoot();
            return;
        }

        // 그 외의 경우에만 이동 (근거리거나, 범위 밖일 때)
        Vector2 direction = (main.Target.position - transform.position).normalized;
        main.rb.velocity = direction * main.Data.moveSpeed;

        // 스프라이트 뒤집기
        if (direction.x > 0.01f)
            main.sr.flipX = false;
        else if (direction.x < -0.01f)
            main.sr.flipX = true;
    }
}
