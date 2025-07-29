using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndCollider : MonoBehaviour

{
    public RDWExperimentManager experimentManager;
    public int runNumber;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            Debug.Log("Collider Hit");
            if (runNumber == 1)
                experimentManager.OnReachedEndOfRun1();
        }
    }
}
