using UnityEngine;

/// <summary>
/// Accroché à une lumière : raycast vers la Shadow dans sa direction.
/// S'il n'y a pas d'obstacle entre les deux (premier hit = Shadow), la Shadow est tuée et remplacée par le Mask.
/// Limitation en angle de balayage : au-delà de maxAngle par rapport à forward, pas de raycast (Gizmo pour calibrer).
/// </summary>
public class LightProjector : MonoBehaviour
{
    [SerializeField] private float maxDistance = 50f;

    [Header("Balayage (angle)")]
    [SerializeField] private Vector2 forward = Vector2.down;
    [SerializeField] [Range(0f, 180f)] private float maxAngle = 45f;

    void Update()
    {
        var entities = EntitiesController.Instance;
        if (entities == null) return;

        Vector2 origin = transform.position;
        Vector2 fwd = forward.normalized;

        // Shadow : raycast vers la Shadow, si premier hit = Shadow → tuer (remplacer par Mask)
        var shadow = entities.GetShadow();
        if (shadow != null && shadow.gameObject.activeInHierarchy)
        {
            Vector2 toShadow = (Vector2)shadow.transform.position - origin;
            float distToShadow = toShadow.magnitude;
            if (distToShadow >= 0.01f)
            {
                Vector2 dir = toShadow / distToShadow;
                float angle = Vector2.Angle(fwd, dir);
                if (angle <= maxAngle)
                {
                    float castDistance = Mathf.Min(distToShadow, maxDistance);
                    RaycastHit2D hit = Physics2D.Raycast(origin, dir, castDistance);
                    if (hit.collider != null && hit.collider.GetComponentInParent<ShadowController>() != null)
                    {
                        Debug.Log("Shadow tuée par la lumière");
                        entities.ReplaceShadowByMask(shadow.transform.position);
                    }
                }
            }
        }

        // Mask : raycast vers le Mask, si premier hit = Mask → informer "mask dans lumière"
        bool maskInLight = false;
        var mask = entities.GetMask();
        if (mask != null && mask.gameObject.activeInHierarchy)
        {
            Vector2 toMask = (Vector2)mask.transform.position - origin;
            float distToMask = toMask.magnitude;
            if (distToMask >= 0.01f)
            {
                Vector2 dir = toMask / distToMask;
                float angle = Vector2.Angle(fwd, dir);
                if (angle <= maxAngle)
                {
                    float castDistance = Mathf.Min(distToMask, maxDistance);
                    RaycastHit2D hit = Physics2D.Raycast(origin, dir, castDistance);
                    if (hit.collider != null && hit.collider.GetComponentInParent<MaskController>() != null)
                        maskInLight = true;
                }
            }
        }
        entities.SetMaskInLight(maskInLight);
    }

    void OnDrawGizmos()
    {
        Vector2 origin = transform.position;
        Vector2 fwd = forward.normalized;

        // Cône de balayage
        float halfAngleRad = maxAngle * Mathf.Deg2Rad;
        Vector2 dirLeft = RotateVector2(fwd, halfAngleRad);
        Vector2 dirRight = RotateVector2(fwd, -halfAngleRad);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, dirLeft * maxDistance);
        Gizmos.DrawRay(origin, dirRight * maxDistance);

        var entities = EntitiesController.Instance;
        if (entities == null) return;

        // Raycast vers Shadow
        var shadow = entities.GetShadow();
        if (shadow != null && shadow.gameObject.activeInHierarchy)
        {
            Vector2 toShadow = (Vector2)shadow.transform.position - origin;
            float dist = Mathf.Min(toShadow.magnitude, maxDistance);
            if (dist >= 0.01f)
            {
                Vector2 dir = toShadow.normalized;
                float angle = Vector2.Angle(fwd, dir);
                Gizmos.color = angle <= maxAngle ? Color.red : Color.gray;
                Gizmos.DrawRay(origin, dir * dist);
            }
        }

        // Raycast vers Mask
        if (entities.IsMaskInWorld())
        {
            var mask = entities.GetMask();
            if (mask != null)
            {
                Vector2 toMask = (Vector2)mask.transform.position - origin;
                float dist = Mathf.Min(toMask.magnitude, maxDistance);
                if (dist >= 0.01f)
                {
                    Vector2 dir = toMask.normalized;
                    float angle = Vector2.Angle(fwd, dir);
                    Gizmos.color = angle <= maxAngle ? Color.blue : Color.gray;
                    Gizmos.DrawRay(origin, dir * dist);
                }
            }
        }
    }

    private static Vector2 RotateVector2(Vector2 v, float rad)
    {
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
