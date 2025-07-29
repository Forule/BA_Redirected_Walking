using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinCollectible : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("MainCamera"))
    {
        gameObject.SetActive(false);
    }
}
}