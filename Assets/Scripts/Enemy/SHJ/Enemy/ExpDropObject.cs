using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ExpDropObject", menuName = "ScriptableObjects/ExpDropTier")]
public class ExpDropObject : ScriptableObject
{
    public float[] timeExp;

    [Tooltip("경험치 값")]
    public int[] expValues;

    [Header("경험치")]
    public GameObject[] expGemPrefabs;
}
