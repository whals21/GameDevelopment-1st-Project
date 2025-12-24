using UnityEngine;
using System.Collections;

public class FieldItem : MonoBehaviour
{
    public enum ItemType // 아이템 종류
    {
        Magnet,
        Heal,
        Bomb
    }

    [Header("아이템 설정")]
    public ItemType type;
    public float healAmount = 20f; // 힐량
    public float bombDamage = 9999f;
    private bool canPickup = false;

    private void OnEnable()
    {
        canPickup = false; // 잠금
        StartCoroutine(EnablePickupRoutine()); // 쿨타임 시작
    }

    IEnumerator EnablePickupRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 0.5초 대기
        canPickup = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canPickup) return;

        if (collision.CompareTag("Player"))
        {
            ApplyEffect();
            Destroy(gameObject);
        }
    }

    // 아이템 효과 발동
    private void ApplyEffect()
    {
        switch (type)
        {
            case ItemType.Magnet:
                Debug.Log("자석 발동, 경험치 끌어오기");

                // "Exp" 태그를 가진 모든 경험치들을 찾기
                GameObject[] gems = GameObject.FindGameObjectsWithTag("Exp");

                foreach (GameObject gem in gems)
                {
                    ExpGem expScript = gem.GetComponent<ExpGem>();

                    //  Magnetize 실행
                    if (expScript != null)
                    {
                        expScript.Magnetize();
                    }
                }
                break;

            case ItemType.Heal:
                Debug.Log("체력 회복");

                if (GameManager.Instance.player != null)
                {
                    PlayerStats playerStats = GameManager.Instance.player.GetComponent<PlayerStats>();

                    if (playerStats != null)
                    {
                        playerStats.Heal(healAmount); // 아까 만든 함수 호출!
                    }
                }
                break;

            case ItemType.Bomb:
                Debug.Log("폭탄 발동");

                if (PlayerHUD.Instance != null)
                {
                    PlayerHUD.Instance.TriggerFlashEffect();
                }
                // 맵에 있는 모든 Enemy 태그를 찾음
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

                // 하나씩 데미지를 줌
                foreach (GameObject enemyObj in enemies)
                {
                    Enemy enemyScript = enemyObj.GetComponent<Enemy>();

                    if (enemyScript != null)
                    {
                        // TakeDamage 함수 호출
                        enemyScript.TakeDamage(bombDamage, true);
                    }
                }
                break;
        }
    }
}