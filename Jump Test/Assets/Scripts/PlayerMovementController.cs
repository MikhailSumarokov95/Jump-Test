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
    private float horizontalSpeedPlayers;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed;
    private float rotationInput;

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
        walkingForward,
        walkingBack,
        walkingRigth,
        walkingLeft,
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
            capsuleCollider.height / 2 + 0.2f, WhatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();
        if (jumpInput) Jump();
        if (somersaultInput) StartCoroutine(Somersault());
        MovePlayer();
        RotatePlayer();

        //handle drag
        if (grounded) rb.drag = groundDrag;
        else rb.drag = 0;
    }

    private void MyInput()
    {
        rotationInput = Input.GetAxis("Mouse X");
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        jumpInput = Input.GetKeyDown(jumpKey);
        somersaultInput = Input.GetKeyDown(somersaultKey);
    }

    private void StateHandler()
    {
        //Mode = Somersault
        if (isStartedCoroutineSomersault) state = MovementState.somersault;

        //Mode = jump
        else if (!grounded)
        {
            state = MovementState.jump;
            if (!rb.useGravity) rb.useGravity = true;
        }

        //Mode - Stay
        else if (rb.velocity.magnitude < 1f)
        {
            state = MovementState.stay;
        }

        //Mode - Sprinting
        else if (Input.GetKey(sprintKey) &&
            verticalInput > 0.01 && Mathf.Approximately(horizontalInput, 0))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        //Mode - Walking
        else
        {
            moveSpeed = walkSpeed;
            if (verticalInput > 0.01f)
                state = MovementState.walkingForward;
            else if (verticalInput < -0.01f)
                state = MovementState.walkingBack;
            else if (horizontalInput > 0.01f)
                state = MovementState.walkingRigth;
            else if (horizontalInput < -0.01f)
                state = MovementState.walkingLeft;
        }

        ActivateAnimation(state);
    }

    private void MovePlayer()
    {
        if (state == MovementState.jump || state == MovementState.somersault) return;

        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //on slope
        if (OnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);
            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * moveSpeed * 10f, ForceMode.Force);
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

    private void RotatePlayer()
    {
        if (state == MovementState.jump || state == MovementState.somersault) return;
        transform.Rotate(Vector3.up, rotationInput * rotationSpeed);
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
            return angle < maxSlopeAngle && !Mathf.Approximately(angle, 0);
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void ActivateAnimation(MovementState newState)
    {
        horizontalSpeedPlayers = new Vector2(rb.velocity.x, rb.velocity.z).sqrMagnitude;
        animator.SetFloat("SpeedPlayers", horizontalSpeedPlayers);
        if (newState == actualState) return;
        actualState = newState;
        switch (newState)
        {
            case MovementState.stay:
                animator.SetTrigger("OnStay");
                break;
            case MovementState.walkingForward:
                animator.SetTrigger("OnWalkForward");
                break;
            case MovementState.sprinting:
                animator.SetTrigger("OnWalkForward");
                break;            
            case MovementState.walkingBack:
                animator.SetTrigger("OnWalkBack");
                break;            
            case MovementState.walkingLeft:
                animator.SetTrigger("OnWalkLeft");
                break;            
            case MovementState.walkingRigth:
                animator.SetTrigger("OnWalkRigth");
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
        var lengthAnimations = 2.533f;
        var timerAnimation = 0f;
        while (timerAnimation < lengthAnimations)
        {
            if (OnSlope())
            {
                var directionSomersaultOnSlope = Vector3.ProjectOnPlane(orientation.forward, slopeHit.normal).normalized;
                rb.AddForce(directionSomersaultOnSlope * somersaultForce, ForceMode.Force);
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
