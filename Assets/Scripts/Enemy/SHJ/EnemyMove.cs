using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
<<<<<<< HEAD
    private Enemy main;
    
    private void Awake()
    {
        main = GetComponent<Enemy>();
=======
    private EnemyCore main;
    
    private void Awake()
    {
        main = GetComponent<EnemyCore>();
>>>>>>> 8f924e2fe6fe03403d31031d38a56b12d1632d97
       
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

        // �̵�
        main.rb.velocity = direction * main.Data.moveSpeed;

        // �¿� ����
        if (direction.x > 0.01f)
            main.sr.flipX = false;
        else if (direction.x < -0.01f)
            main.sr.flipX = true;

    }
}
