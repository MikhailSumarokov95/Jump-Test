using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float groundDrag;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float jumpForce;
    [SerializeField] private float somersaultForce;
    [SerializeField] private float maxSlopeAngle;
    private bool isStartedCoroutineSomersault;

    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode somersaultKey = KeyCode.LeftAlt;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask WhatIsGround;
    public bool grounded;

    public Transform orientation;

    private Animator animator;

    float horizontalInput;
    float verticalInput;
    private bool jumpInput;
    private bool somersaultInput;

    Vector3 moveDirection;
    private RaycastHit slopeHit;

    Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    public MovementState state;
    private MovementState actualState;

    public enum MovementState
    {
        stay,
        walking,
        sprinting,
        jump,
        somersault
    }

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        capsuleCollider = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();
        moveSpeed = walkSpeed;
    }

    // Update is called once per frame
    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(capsuleCollider.center + transform.position, Vector3.down,
            capsuleCollider.height / 2 + 0.1f, WhatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();
        
        if (jumpInput) Jump();
        if (somersaultInput) StartCoroutine(Somersault());

        //handle drag
        if (grounded) rb.drag = groundDrag;
        else rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        jumpInput = Input.GetKeyDown(jumpKey);
        somersaultInput = Input.GetKeyDown(somersaultKey);
    }

    private void StateHandler()
    {
        //Mode = Somersault
        if (somersaultInput || isStartedCoroutineSomersault) state = MovementState.somersault;

        //Mode = jump
        else if (!grounded) state = MovementState.jump;

        //Mode - Stay
        else if (rb.velocity.magnitude < 1f) state = MovementState.stay;

        //Mode - Sprinting
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        //Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        ActivateAnimation(state);
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //on slope
        if (OnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if(rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        //on ground
        else if(grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }

        //in air
        else if(!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        //turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }

    private void Jump()
    {
        if (state != MovementState.jump && state != MovementState.somersault)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void SpeedControl()
    {

        //limit speed on slope
        if(OnSlope())
        {
            if(rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        //limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            //limit velocity if needed
            if(flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }     
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void ActivateAnimation(MovementState newState)
    {
        if (newState == actualState) return;
        actualState = newState;
        switch (newState)
        {
            case MovementState.stay:
                animator.SetTrigger("OnStay");
                break;
            case MovementState.walking:
                animator.SetTrigger("OnWalk");
                animator.speed = 1f;
                break;
            case MovementState.sprinting:
                animator.SetTrigger("OnWalk");
                animator.speed = sprintSpeed / walkSpeed;
                break;
            case MovementState.jump:
                animator.SetTrigger("OnJump");
                break;
            case MovementState.somersault:
                animator.SetTrigger("OnSomersault");
                break;
        }
    }

    private IEnumerator Somersault()
    {
        if (state == MovementState.jump || isStartedCoroutineSomersault) yield break;

        isStartedCoroutineSomersault = true;

        var timerAnimation = 0f;
        while (timerAnimation < 2f)
        {
            if (OnSlope())
            {
                var directionSomersaultOnSlope = Vector3.ProjectOnPlane(orientation.forward, slopeHit.normal).normalized;
                rb.AddForce(directionSomersaultOnSlope * 20f * somersaultForce, ForceMode.Force);
            }

            else
            {
                rb.AddForce(orientation.forward * somersaultForce, ForceMode.Force);
            }
            timerAnimation += Time.deltaTime;
            yield return null;
        }
        isStartedCoroutineSomersault = false;
    }
}
