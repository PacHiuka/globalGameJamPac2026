using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Crée et gère les instances Masque / Ombre. Centralise les inputs et les transmet à l'entité active.
/// </summary>
public class EntitiesController : MonoBehaviour
{
    public static EntitiesController Instance { get; private set; }

    [SerializeField] private GameObject maskPrefab;
    [SerializeField] private GameObject shadowPrefab;

    private GameObject _maskInstance;
    private ShadowController _shadowInstance;

    public MaskController GetMask() => _maskInstance != null ? _maskInstance.GetComponent<MaskController>() : null;
    public GameObject MaskInstance => _maskInstance;
    public ShadowController GetShadow() => _shadowInstance;
    public bool IsMaskInWorld() => _maskInstance != null && _maskInstance.activeInHierarchy;

    /// <summary>True si l'ombre existe et est l'entité active (visible et contrôlée).</summary>
    private bool IsShadowActive => _shadowInstance != null && _shadowInstance.gameObject.activeInHierarchy;

    /// <summary>Transform de l'entité actuellement contrôlée (Shadow ou Mask), pour la caméra.</summary>
    public Transform GetCurrentTargetTransform()
    {
        if (IsShadowActive) return _shadowInstance.transform;
        if (IsMaskInWorld()) return _maskInstance.transform;
        return null;
    }

    private PlayerCameraController _playerCameraController;

    private void NotifyCameraTarget() => _playerCameraController?.SetTarget(GetCurrentTargetTransform());

    [Header("Game Over (mask dans lumière + quasi immobile)")]
    [SerializeField] private float lostDelay = 2f;
    [SerializeField] private float lostVelocityThreshold = 0.5f;

    private bool _maskInLight;
    private float _lostTimer;

    /// <summary>True si le Mask est touché par la lumière (informé par LightProjector).</summary>
    public bool IsMaskInLight => _maskInLight;

    /// <summary>Appelé par LightProjector chaque frame : le Mask est-il touché par la lumière.</summary>
    public void SetMaskInLight(bool inLight)
    {
        if (inLight && !_maskInLight)
            Debug.Log("Mask sous le soleil");
        if (!inLight && _maskInLight)
            Debug.Log("Mask hors du soleil");
        _maskInLight = inLight;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        FindOrCreateMask();
        FindOrCreateShadow();

        _playerCameraController = FindFirstObjectByType<PlayerCameraController>();
        NotifyCameraTarget();
    }

    void Update()
    {
        if (!IsMaskInWorld() || IsShadowActive) { _lostTimer = 0f; return; }
        if (!_maskInLight) { _lostTimer = 0f; return; }

        var rb = _maskInstance != null ? _maskInstance.GetComponent<Rigidbody2D>() : null;
        if (rb == null) { _lostTimer = 0f; return; }
        if (rb.linearVelocity.sqrMagnitude > lostVelocityThreshold * lostVelocityThreshold) { _lostTimer = 0f; return; }

        _lostTimer += Time.deltaTime;
        if (_lostTimer >= lostDelay)
            ReloadScene();
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void FindOrCreateMask()
    {
        // Cherche un Mask en scène (active ou inactive)
        var maskController = FindFirstObjectByType<MaskController>(FindObjectsInactive.Include);
        if (maskController != null)
        {
            _maskInstance = maskController.gameObject;
            return;
        }

        // Aucun Mask trouvé : on le crée depuis le prefab si disponible
        if (maskPrefab != null)
        {
            _maskInstance = Instantiate(maskPrefab);
            _maskInstance.SetActive(false);
        }
        else
            Debug.LogWarning("[EntitiesController] Aucun Mask en scène et pas de maskPrefab assigné.");
    }

    private void FindOrCreateShadow()
    {
        // Cherche une Shadow en scène (active ou inactive)
        _shadowInstance = FindFirstObjectByType<ShadowController>(FindObjectsInactive.Include);
        if (_shadowInstance != null)
            return;

        // Aucune Shadow trouvée : on la crée depuis le prefab si disponible
        if (shadowPrefab != null)
        {
            var go = Instantiate(shadowPrefab);
            _shadowInstance = go.GetComponent<ShadowController>();
            if (_shadowInstance != null)
                go.SetActive(false);
            else
                Debug.LogWarning("[EntitiesController] Shadow prefab n'a pas de ShadowController.");
        }
        else
            Debug.LogWarning("[EntitiesController] Aucune Shadow en scène et pas de shadowPrefab assigné.");
    }

    /// <summary>Lance le masque (enable). Si hideShadow, désactive l'ombre (sinon l'ombre reste pour finir son animation).</summary>
    public void ThrowMask(Vector3 position, Vector2 velocity, bool hideShadow = true)
    {
        if (_maskInstance == null) return;

        _maskInstance.transform.position = position;
        _maskInstance.SetActive(true);

        var rb = _maskInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = velocity;

        if (hideShadow && _shadowInstance != null)
            _shadowInstance.gameObject.SetActive(false);
        NotifyCameraTarget();
    }

    /// <summary>Désactive l'ombre (après fin de l'animation de shot).</summary>
    public void DisableShadow()
    {
        if (_shadowInstance != null)
            _shadowInstance.gameObject.SetActive(false);
        NotifyCameraTarget();
    }

    /// <summary>Récupère le masque (disable). L'ombre reste active.</summary>
    public void CatchMask()
    {
        if (_maskInstance != null)
            _maskInstance.SetActive(false);
        NotifyCameraTarget();
    }

    /// <summary>Remplace le masque par l'ombre à la position donnée (enable shadow, disable mask).</summary>
    public void FormShadow(Vector3 position)
    {
        if (_shadowInstance == null) return;

        _shadowInstance.transform.position = position;
        _shadowInstance.gameObject.SetActive(true);

        if (_maskInstance != null)
            _maskInstance.SetActive(false);
        NotifyCameraTarget();
    }

    /// <summary>Shadow touchée par la lumière : on la remplace par le mask à la position (sans shot).</summary>
    public void ReplaceShadowByMask(Vector3 position)
    {
        if (_maskInstance == null) return;

        _maskInstance.transform.position = position;
        _maskInstance.SetActive(true);

        var rb = _maskInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (_shadowInstance != null)
            _shadowInstance.gameObject.SetActive(false);
        NotifyCameraTarget();
    }

#region Input (centralisé, transmis à l'entité active)

    void OnMove(InputValue value)
    {
        if (IsShadowActive)
            _shadowInstance.ReceiveMove(value.Get<Vector2>());
        else if (IsMaskInWorld())
            GetMask().ReceiveMove(value.Get<Vector2>());
    }

    void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        if (IsShadowActive)
            _shadowInstance.ReceiveJump();
        else if (IsMaskInWorld())
            GetMask().ReceiveJump();
    }

    void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        if (IsShadowActive)
            _shadowInstance.ReceiveAction();
        else if (IsMaskInWorld())
            GetMask().ReceiveAction();
    }

    void OnCrouch(InputValue value)
    {
        if (value.isPressed)
            ReloadScene();
    }

#endregion
}
