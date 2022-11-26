using UnityEngine;

public class CameraThirdPerson : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float damping = 1f;
    [SerializeField] private Vector3 offset = new Vector3(0, 4.7f, -5.8f);

    private void LateUpdate()
    {
        var currentPosition = transform.position;
        var desiredPosition = _target.position + (_target.rotation * offset);
        transform.position = Vector3.Lerp(currentPosition, desiredPosition, Time.deltaTime * damping);
        transform.LookAt(_target.transform);
    }
}
