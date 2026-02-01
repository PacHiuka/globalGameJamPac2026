using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private GameObject cameraPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothSpeed = 5f;

    private GameObject _cameraInstance;
    private Transform _currentTarget;

    public void SetTarget(Transform target) => _currentTarget = target;

    void Start()
    {
        if (cameraPrefab != null)
            _cameraInstance = Instantiate(cameraPrefab);
    }

    void LateUpdate()
    {
        if (_currentTarget == null || _cameraInstance == null) return;

        Vector3 desiredPosition = _currentTarget.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(_cameraInstance.transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        _cameraInstance.transform.position = smoothedPosition;
    }
}
