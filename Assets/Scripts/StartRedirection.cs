using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartRedirection : MonoBehaviour
{
    // Dieses Flag kannst du in anderen Scripten abfragen
    public bool isRedirectionActive = false;

    // Diese Methode kannst du im Button-OnClick() im Inspector zuweisen!
    public void StartRedirectionNow()
    {
        isRedirectionActive = true;
        Debug.Log("Redirection aktiviert!");
    }
}
