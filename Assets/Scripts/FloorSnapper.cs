using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorSnapper : MonoBehaviour
{
    public Transform xrRig;           
    public Transform mainCamera;      
    public LayerMask groundLayer;     
    public float raycastDistance = 5f;

    void LateUpdate()
    {
        SnapToFloor();
    }

    void SnapToFloor()
    {
        
        Vector3 rayOrigin = new Vector3(mainCamera.position.x, mainCamera.position.y + 0.2f, mainCamera.position.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
           
            float cameraHeight = mainCamera.localPosition.y;

            
            float targetY = hit.point.y - cameraHeight;

            xrRig.position = new Vector3(xrRig.position.x, targetY, xrRig.position.z);
        }
    }
}
