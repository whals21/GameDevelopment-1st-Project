using UnityEngine;
using System.Collections;

public class FieldItem : MonoBehaviour
{
    public enum ItemType // ������ ����
    {
        Magnet,
        Heal,
        Bomb
    }

    [Header("������ ����")]
    public ItemType type;
    public float healAmount = 20f; // ����
    public float bombDamage = 9999f;
    private bool canPickup = false;

    private void OnEnable()
    {
        canPickup = false; // ���
        StartCoroutine(EnablePickupRoutine()); // ��Ÿ�� ����
    }

    IEnumerator EnablePickupRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 0.5�� ���
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

    // ������ ȿ�� �ߵ�
    private void ApplyEffect()
    {
        switch (type)
        {
            case ItemType.Magnet:
                Debug.Log("�ڼ� �ߵ�, ����ġ �������");

                // "Exp" �±׸� ���� ��� ����ġ���� ã��
                GameObject[] gems = GameObject.FindGameObjectsWithTag("Exp");

                foreach (GameObject gem in gems)
                {
                    ExpGem expScript = gem.GetComponent<ExpGem>();

                    //  Magnetize ����
                    if (expScript != null)
                    {
                        expScript.Magnetize();
                    }
                }
                break;

            case ItemType.Heal:
                Debug.Log("ü�� ȸ��");

                if (GameManager.Instance.player != null)
                {
                    PlayerStats playerStats = GameManager.Instance.player.GetComponent<PlayerStats>();

                    if (playerStats != null)
                    {
                        playerStats.Heal(healAmount); // �Ʊ� ���� �Լ� ȣ��!
                    }
                }
                break;

            case ItemType.Bomb:
                Debug.Log("��ź �ߵ�");

                if (PlayerHUD.Instance != null)
                {
                    PlayerHUD.Instance.TriggerFlashEffect();
                }
                // �ʿ� �ִ� ��� Enemy �±׸� ã��
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

                // �ϳ��� �������� ��
                foreach (GameObject enemyObj in enemies)
                {
                    Enemy enemyScript = enemyObj.GetComponent<Enemy>();

                    if (enemyScript != null)
                    {
                        // TakeDamage 함수 호출 (source=null, isCritical=true)_조민희
                        enemyScript.TakeDamage(bombDamage, null, true);
                    }
                }
                break;
        }
    }
}