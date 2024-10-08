using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    #region Movement
    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 1;
    private float xAxis, yAxis;
    #endregion

    #region Jumping Setting
    [Header("Jumping Setting")]
    [SerializeField] private float jumpForce;
    private float jumpBufferCounter = 0;
    [SerializeField] private float jumpBufferFrames;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJump;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    [Space(5)]
    #endregion

    #region Ground Check
    [Header("Grounded Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;
    [Space(5)]
    #endregion

    #region Dash Setting
    [Header("Dash System")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] GameObject dashEffect;
    [Space(5)]
    #endregion

    #region Attack Setting
    [Header("Attack Setting")]
    bool attack = false;
    float timeBetweenAttack, timeSinceAttack;
    [SerializeField] private Transform sideAttackTransform, upAttackTransform, downAttackTransform;
    [SerializeField] private Vector2 sideAttackArea, upAttackArea, downAttackArea;
    [SerializeField] private LayerMask attackableLayer;
    [SerializeField] private float damage;
    [SerializeField] GameObject slashEffect;
    [Space(5)]
    #endregion

    #region Override Func
    PlayerStateList pState;
    #endregion
    
    #region Component
    private Rigidbody2D rb;
    Animator anim;
    #endregion

    #region value Other
    private float gravity;
    private bool canDash = true;
    private bool Dashed = false;
    #endregion

    

    public static Player_Controller Instance;

#region Unity Func
    private void Awake() {
        if(Instance != null && Instance != this){
            Destroy(gameObject);
        }else{
            Instance = this;
        }
    }

    private void Start() {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        gravity = rb.gravityScale;

        // canDash = true;
        // Dashed = false;
    }

    private void Update() {
        GetInputs();
        UpdateJumpVariables();
        if (pState.dashing) return;
        Flip();
        Move();
        Jump();
        StartDash();
        Attack();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(sideAttackTransform.position, sideAttackArea);
        Gizmos.DrawWireCube(upAttackTransform.position, upAttackArea);
        Gizmos.DrawWireCube(downAttackTransform.position, downAttackArea);
    }
#endregion

#region Movevement Func
    private void GetInputs(){
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetMouseButtonDown(0);
    }

    void Flip()
    {
        if(xAxis < 0)
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
        } else {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
        }
    }

    private void Move(){
        rb.velocity = new Vector2(walkSpeed * xAxis, rb.velocity.y);
        anim.SetBool("Walking", rb.velocity.x != 0 && Grounded());
    }
#endregion

#region Dash Func
    void StartDash(){
        if(Input.GetButtonDown("Dash") && canDash && !Dashed){
            StartCoroutine(Dash());
            Dashed = true;
        }

        if (Grounded()){
            Dashed = false;
        }
    }

    IEnumerator Dash(){
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if(Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
#endregion

#region Attack Func
void Attack(){
    timeSinceAttack += Time.deltaTime;
    if(attack && timeSinceAttack >= timeBetweenAttack){
        timeSinceAttack = 0;
        anim.SetTrigger("Attacking");

        if(yAxis == 0 || yAxis < 0 && Grounded()){
            Hit(sideAttackTransform, sideAttackArea);
            Instantiate(slashEffect, sideAttackTransform);
        }else if(yAxis > 0){
            Hit(upAttackTransform, upAttackArea);
            slashEffectAngle(slashEffect, 80, upAttackTransform);
        }else if(yAxis < 0 && !Grounded()){
            Hit(downAttackTransform, downAttackArea);
            slashEffectAngle(slashEffect, -90, downAttackTransform);
        }
    }
}

private void Hit(Transform _attackTrasform, Vector2 _attackArea){
    Collider2D[] objectToHit = Physics2D.OverlapBoxAll(_attackTrasform.position, _attackArea, 0, attackableLayer);

    if(objectToHit.Length > 0){
        Debug.Log("Hit");
    }
    for(int i = 0; i < objectToHit.Length; i++){
        if(objectToHit[i].GetComponent<Enemy>() != null){
            objectToHit[i].GetComponent<Enemy>().EnemyHit(damage, (transform.position - objectToHit[i].transform.position).normalized, 100);
        }
    }
}

void slashEffectAngle(GameObject _SlashEffect, int _effectAngle, Transform _attackTransform){
    _SlashEffect = Instantiate(_SlashEffect, _attackTransform);
    _SlashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
    _SlashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
}
#endregion

#region Check Grounded
    public bool Grounded(){
        if(Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }else{
            return false;
        }
    }
#endregion

#region Jump Func
    void Jump(){
        if(Input.GetButtonUp("Jump") && rb.velocity.y > 0){
            rb.velocity = new Vector2(rb.velocity.x, 0);
            pState.jumping = false;
        }

        if(!pState.jumping)
        {
            if(jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
                pState.jumping = true;
            } else if(!Grounded() && airJumpCounter < maxAirJump && Input.GetButtonDown("Jump")) {
                pState.jumping = true;

                airJumpCounter++;
                rb.velocity = new Vector3(rb.velocity.x , jumpForce);
            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    // Làm giảm Frame mỗi lần nhảy
    void UpdateJumpVariables(){
        if(Grounded()){
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        } else {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if(Input.GetButtonDown("Jump")){
            jumpBufferCounter = jumpBufferFrames;
        }else{
            jumpBufferCounter = jumpBufferCounter - Time.deltaTime * 10;
        }
    }
#endregion
}
