using UnityEngine;

public class MaskController : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] private float firstBounceRestitution = 1f;

    private bool _firstCollision = true;
    private Vector2 _pendingBounceVelocity;
    private bool _applyBounceNextFixed;
    private Vector2 _velocityBeforePhysics;

    void OnDisable()
    {
        _firstCollision = true;
        _applyBounceNextFixed = false;
    }

    void FixedUpdate()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            _velocityBeforePhysics = rb.linearVelocity;

        if (_applyBounceNextFixed && rb != null)
        {
            rb.linearVelocity = _pendingBounceVelocity;
            _applyBounceNextFixed = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_firstCollision) return;

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        if (collision.contactCount == 0) return;

        // Unity: normal pointe du point de contact VERS l'autre collider → on inverse pour avoir "vers l'extérieur"
        Vector2 normal = -collision.GetContact(0).normal;
        normal.Normalize();

        // Vélocité d'AVANT la collision (celle du début de ce FixedUpdate), pas celle déjà modifiée par la physique
        Vector2 v = _velocityBeforePhysics;
        Vector2 reflected = v - 2f * Vector2.Dot(v, normal) * normal;
        _pendingBounceVelocity = reflected * firstBounceRestitution;
        _applyBounceNextFixed = true;
        _firstCollision = false;
    }

    /// <summary>Appelé par EntitiesController quand le masque est l'entité active.</summary>
    public void ReceiveMove(Vector2 value) { }

    public void ReceiveJump() { }

    public void ReceiveAction()
    {
        var entities = EntitiesController.Instance;
        if (entities == null || entities.IsMaskInLight) {
            return;
        }

        entities.FormShadow(transform.position);
    }
}
