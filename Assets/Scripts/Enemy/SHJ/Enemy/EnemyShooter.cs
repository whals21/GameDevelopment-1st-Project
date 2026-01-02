using System.Collections;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    private Enemy enemy;
    private float lastShootTime = -999f;
    private void Awake()
    {
        enemy = GetComponent<Enemy>();

       
        
    }
    public void TryShoot()
    {
        if (!enemy.IsRange) return;
        if (enemy.Target == null) return;

        // 쿨타임 체크
        if (Time.time < lastShootTime + enemy.AttackDelay)
            return;

        Shoot();
        lastShootTime = Time.time;
    }
    private void Start()
    {
        
    }

    // 모든 로직이 여기 안에 있음. Update() 필요 없음!
    private void Shoot()
    {
      

        EnemyBullet bullet = ObjectPoolManager.Instance.GetEnemyBullet();
        if (bullet == null) return;

        bullet.transform.position = enemy.point.position;

        // 적의 목표 위치(Target.position)에서 현재 위치(point.position)를 뺀 방향 벡터를 구한다.
        Vector2 dir = (enemy.Target.position - enemy.point.position).normalized;
        // Mathf.Atan2(y, x)는 x축을 기준으로 한 반시계 방향 각도를 반환한다.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        bullet.gameObject.SetActive(true);
    }
   
}