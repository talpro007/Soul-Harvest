using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform targetPoint;
    [Header("higher smoothTime = smoother")]
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (targetPoint == null) return;

        Vector3 targetPos = new Vector3(
            targetPoint.position.x,
            targetPoint.position.y,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}
