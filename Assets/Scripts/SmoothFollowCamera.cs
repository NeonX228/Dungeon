using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0f, 5f, -10f);
    public float followSpeed = 5f;

    [Header("Optional Rotation Follow")]
    public bool followRotation = false;
    public float rotationSpeed = 5f;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("No target assigned!");
            enabled = false;
            return;
        }

        // Immediately snap to target offset at start
        transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Smooth position follow
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        if (followRotation)
        {
            Quaternion desiredRotation = target.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }
    }
}