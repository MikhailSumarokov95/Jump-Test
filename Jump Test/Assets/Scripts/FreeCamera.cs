using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset;
    [SerializeField] float speedMove = 1f;
    private Vector3 angle;

    private void LateUpdate()
    {
        angle += new Vector3(0, Input.GetAxisRaw("Mouse X"), 0) * speedMove;
        transform.position = target.position + Quaternion.Euler(angle) * offset;
        transform.LookAt(target);
    }
}
