using System.Collections;
using UnityEngine;

public class ShadowController : MonoBehaviour
{

#region Variables
    [SerializeField] private bool grounded;
    [SerializeField] private bool onWall;
    [SerializeField] private bool onWallLeft;
    [SerializeField] private bool onWallRight;
    [SerializeField] private bool lookRight;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D col2d;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [SerializeField] private float groundRaycastLength;
    [SerializeField] private float groundRaycastWidth;
    [SerializeField] private float groundRaycastOffset;
    [SerializeField] private int groundRaycastCount;

    [SerializeField] private float wallRaycastLength;
    [SerializeField] private float wallRaycastHeight;
    [SerializeField] private float wallRaycastOffset;
    [SerializeField] private int wallRaycastCount;

    [SerializeField] private float moveSpeedOnGround = 25f;
    [SerializeField] private float moveSpeedOnGroundLerp = 0.99f;
    [SerializeField] private float moveSpeedInAir = 20f;
    [SerializeField] private float moveSpeedInAirLerp = 0.5f;
    [SerializeField] private float jumpForceOnGround = 10f;
    [SerializeField] private Vector2 jumpForceOnWall = new Vector2(5f, 5f);
    [SerializeField] private float wallSlideSpeed = 2f;

    [SerializeField] private Vector2 moveInput = Vector2.zero;

    [SerializeField] private Vector2 throwVelocity = new Vector2(12f, 4f);
    [SerializeField] private float throwOffset = 0.5f;

    private bool _controlsDisabled;
    private const float ShotDelay = 0.25f;
    private const float ShotAnimationDuration = 1f;
#endregion

#region Unity Functions

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (col2d == null) col2d = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        moveInput = Vector2.zero;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (animator == null) animator = GetComponent<Animator>();
        if (animator != null)
            animator.Rebind();
    }

    void Update()
    {
        if (rb == null) return;
        VerifyState();
        if (!_controlsDisabled)
            Move();
        SetAnimatorParameters();
    }

#endregion

#region Input

    public void ReceiveMove(Vector2 value)
    {
        if (_controlsDisabled) return;
        moveInput = value;
    }

    public void ReceiveJump()
    {
        if (_controlsDisabled) return;
        Jump();
    }

    public void ReceiveAction()
    {
        if (_controlsDisabled) return;

        var entities = EntitiesController.Instance;
        if (entities != null && entities.IsMaskInWorld())
        {
            shot();
            return;
        }

        StartCoroutine(ShotSequence());
    }

#endregion

#region Action Functions

    private void Move()
    {
        if (grounded)
        {
            MoveOnGround();
        }
        if (onWall)
        {
            if (lookRight && onWallRight) turnAround();
            else if (!lookRight && onWallLeft) turnAround();
            MoveOnWall();
        }
        else
        {
            MoveInAir();
        }
    }

    private void MoveOnGround()
    {
        if(moveInput.x == 0)
        {
            SetAnimatorRun(false);
            float targetVelocityX = 0;
            float lerpedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, moveSpeedOnGroundLerp * Time.deltaTime);
            rb.linearVelocity = new Vector2(lerpedVelocityX, rb.linearVelocity.y);
        }
        else
        {
            bool didTurn = false;
            SetAnimatorRun(true);
            if(lookRight && moveInput.x < 0)
            {
                turnAround();
                didTurn = true;
            }
            if(!lookRight && moveInput.x > 0)
            {
                turnAround();
                didTurn = true;
            }
            if(didTurn)
            {
                rb.linearVelocity = new Vector2(moveInput.x * moveSpeedOnGround, rb.linearVelocity.y);
            }
            else
            {
                float targetVelocityX = moveInput.x * moveSpeedOnGround;
                float lerpedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, moveSpeedOnGroundLerp * Time.deltaTime);
                rb.linearVelocity = new Vector2(lerpedVelocityX, rb.linearVelocity.y);
            }
        }
    }
    private void MoveOnWall()
    {
        Vector2 vel = rb.linearVelocity;
        if (vel.y < -wallSlideSpeed)
        {
            vel.y = -wallSlideSpeed;
            rb.linearVelocity = vel;
        }
    }
    private void MoveInAir()
    {
        float targetVelocityX = moveInput.x * moveSpeedInAir;
        float lerpedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, moveSpeedInAirLerp * Time.deltaTime);
        rb.linearVelocity = new Vector2(lerpedVelocityX, rb.linearVelocity.y);
    }

    private void Jump()
    {
        if (grounded)
        {
            JumpOnGround();
        }
        if (onWall)
        {
            JumpOnWall();
        }
    }

    private void JumpOnGround()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForceOnGround);
    }
    private void JumpOnWall()
    {
        rb.linearVelocity = new Vector2(jumpForceOnWall.x * (lookRight ? 1 : -1), jumpForceOnWall.y);
    }

    private void turnAround()
    {
        lookRight = !lookRight;
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }

    private IEnumerator ShotSequence()
    {
        _controlsDisabled = true;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
        if (col2d != null) col2d.enabled = false;

        SetAnimatorAttack(true);

        yield return new WaitForSeconds(ShotDelay);

        DoThrowMask(hideShadow: false);

        yield return new WaitForSeconds(ShotAnimationDuration - ShotDelay);

        SetAnimatorAttack(false);
        _controlsDisabled = false;
        if (col2d != null) col2d.enabled = true;
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;

        var entities = EntitiesController.Instance;
        if (entities != null)
            entities.DisableShadow();
    }

    private void DoThrowMask(bool hideShadow)
    {
        var entities = EntitiesController.Instance;
        if (entities == null) return;

        float dir = lookRight ? 1f : -1f;
        Vector3 spawnPos = transform.position + new Vector3(dir * throwOffset, 0f, 0f);
        Vector2 velocity = new Vector2(throwVelocity.x * dir, throwVelocity.y);
        entities.ThrowMask(spawnPos, velocity, hideShadow);
    }

    private void shot()
    {
        var entities = EntitiesController.Instance;
        if (entities == null) return;

        if (entities.IsMaskInWorld())
        {
            entities.CatchMask();
            return;
        }

        DoThrowMask(hideShadow: true);
    }
#endregion

#region States Functions

    private void VerifyState()
    {
        grounded = IsGrounded();
        onWallLeft = IsOnWallSide(Vector2.left);
        onWallRight = IsOnWallSide(Vector2.right);
        onWall = grounded ? false : (onWallLeft || onWallRight);
    }

    private bool IsGrounded()
    {
        for (int i = 0; i < groundRaycastCount; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position + new Vector3( (-groundRaycastWidth / 2) + (groundRaycastWidth * i / (groundRaycastCount - 1)), groundRaycastOffset, 0), Vector2.down, groundRaycastLength, groundLayer);
            if (hit.collider != null)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsOnWallSide(Vector2 direction)
    {
        float signX = direction.x > 0 ? 1f : -1f;
        Vector3 originOffset = new Vector3(signX * wallRaycastOffset, 0f, 0f);
        for (int i = 0; i < wallRaycastCount; i++)
        {
            float y = (wallRaycastCount > 1) ? (-wallRaycastHeight / 2f) + (wallRaycastHeight * i / (wallRaycastCount - 1)) : 0f;
            Vector3 origin = transform.position + originOffset + new Vector3(0f, y, 0f);
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallRaycastLength, wallLayer);
            if (hit.collider != null) return true;
        }
        return false;
    }

#endregion

#region Animator Functions

    private void SetAnimatorParameters()
    {
        if (animator == null) return;
        animator.SetBool("OnGround", grounded);
        animator.SetBool("OnWall", onWall);
        animator.SetFloat("AirX", lookRight ? -rb.linearVelocity.x : rb.linearVelocity.x);
        animator.SetFloat("AirY", rb.linearVelocity.y);
    }

    private void SetAnimatorRun(bool run)
    {
        if (animator == null) return;
        animator.SetBool("Run", run);
    }

    private void SetAnimatorAttack(bool attack)
    {
        if (animator == null) return;
        animator.SetBool("Attack", attack);
    }

#endregion

#region Debug Functions

    private void OnDrawGizmos()
    {
        DrawGroundRaycasts();
        DrawWallRaycasts();
    }

    private void DrawGroundRaycasts()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < groundRaycastCount; i++)
        {
            Gizmos.DrawRay(transform.position + new Vector3( (-groundRaycastWidth / 2) + (groundRaycastWidth * i / (groundRaycastCount - 1)), groundRaycastOffset, 0), Vector2.down * groundRaycastLength);
        }
    }

    private void DrawWallRaycasts()
    {
        float div = Mathf.Max(1, wallRaycastCount - 1);
        Gizmos.color = Color.blue;
        for (int i = 0; i < wallRaycastCount; i++)
        {
            float y = (-wallRaycastHeight / 2f) + (wallRaycastHeight * i / div);
            Vector3 basePos = transform.position + new Vector3(0f, y, 0f);
            Gizmos.DrawRay(basePos + new Vector3(-wallRaycastOffset, 0f, 0f), Vector2.left * wallRaycastLength);
            Gizmos.DrawRay(basePos + new Vector3(wallRaycastOffset, 0f, 0f), Vector2.right * wallRaycastLength);
        }
    }

#endregion
}
