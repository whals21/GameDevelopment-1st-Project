using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Enemy/Enemy Data", fileName = "New Enemy")]
public class EnemyObject : ScriptableObject
{
    [Header("ï¿½ï¿½ï¿½ï¿½")]
    public GameObject prefab;
    [Header("ï¿½Ì¸ï¿½")]
    public string EnemyName;     //ï¿½Ì¸ï¿½
    [Header("Ã¼ï¿½ï¿½")]
    public int EnemyHP;          //ï¿½ï¿½ï¿½ï¿½ Ã¼ï¿½ï¿½

    [Header("ï¿½Ìµï¿½ï¿½Óµï¿½")]
    public float moveSpeed;

<<<<<<< HEAD
    [Header("ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½")]
    public float contactDamage;  // ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½

    [Header("ï¿½ï¿½ï¿½è¿­Ä¡")]
    public int expValue;  // Ä¡ï¿½ï¿½ ï¿½ï¿½ ï¿½ï¿½ï¿½è¿­Ä¡

    [Header("ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½")]
    public WeaponObject[] weapons;
=======
    [Header("=== ¿ø°Å¸® Àü¿ë ===")]
    public bool isRanged = false;                    // ÀÌ ÀûÀÌ ¿ø°Å¸®ÀÎÁö?
    public GameObject projectilePrefab;              // ¹ß»çÃ¼ ÇÁ¸®ÆÕ
    public float attackRange = 8f;                   // °ø°Ý »ç°Å¸®
    public float attackDelay = 2f;                   // °ø°Ý ÄðÅ¸ÀÓ
    public Transform firePoint;                      // ¹ß»ç À§Ä¡ (ÇÁ¸®ÆÕ¿¡ ÁöÁ¤ÇÏ°Å³ª ·±Å¸ÀÓ¿¡¼­ Ã£À½)

    [Header("°ø°ÝÆÐÅÏ")]
    public WeaponObject[] weapons;
    

>>>>>>> 8f924e2fe6fe03403d31031d38a56b12d1632d97
}
