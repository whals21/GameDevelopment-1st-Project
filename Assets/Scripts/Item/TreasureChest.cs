using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // "Player" ÅÂ±×
        if (collision.CompareTag("Player"))
        {
            OpenChest();
        }
    }

    void OpenChest()
    {
        // ·ê·¿
        if (RouletteManager.Instance != null)
        {
            RouletteManager.Instance.ShowRoulette();
        }
        Destroy(gameObject);
    }
}