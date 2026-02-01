using UnityEngine;

/// <summary>
/// Crée la caméra à partir d'un prefab, la réutilise et la fait suivre l'entité que le joueur contrôle.
/// La cible est notifiée par EntitiesController (pas d'appel à EntitiesController chaque frame).
/// </summary>
public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private GameObject cameraPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothSpeed = 5f;

    private GameObject _cameraInstance;
    private Transform _currentTarget;

    /// <summary>Appelé par EntitiesController quand l'entité contrôlée change.</summary>
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
