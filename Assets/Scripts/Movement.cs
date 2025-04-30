using System;
using UnityEngine;
using UnityEngine.Events;

public class Movement : MonoBehaviour
{
    public UnityEvent<Vector3> OnClick;
    private Vector3 lastClickPosition;
    
    // Update is called once per frame
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        var mouseRay = Camera.main.ScreenPointToRay( Input.mousePosition );
        if (Physics.Raycast( mouseRay, out var hitInfo )) {
            lastClickPosition = hitInfo.point;
            OnClick.Invoke(lastClickPosition);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine( transform.position, lastClickPosition );
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastClickPosition, 0.2f);
    }
}
