using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")] public MovementState state;
    public float walkSpeed;
    public float sprintSpeed;
    private float moveSpeed;
    public float slideSpeed;
    public float wallRunSpeed;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    public Transform orientation;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;

    public float groundDrag;


    [Header("Jumping")] public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool readyToJump;


    [Header("Crouching")] public float crouchSpeed;
    public float crouchYScale;
    private float defaultYScale;


    [Header("Keybindings")] public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;


    [Header("Ground Check")] public float playerHeight;
    public LayerMask whatIsGround;
    private bool grounded;


    [Header("Slope Handling")] public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;


    [Header("UI/Debug")] public TextMeshProUGUI speedText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI onSlopeAndAngelText;


    //Store states
    public enum MovementState
    {
        walking,
        sprinting,
        wallrunning,
        crouching,
        sliding,
        onAir
    }

    [HideInInspector] public bool sliding;
    [HideInInspector] public bool wallRunning;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        defaultYScale = transform.localScale.y;
    }

    private void Update()
    {
        //ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f);

        PlayerInput();
        SpeedControl();
        StateHandler();


        //handle drag
        if (grounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }

        //debug speed UI
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        speedText.text = "XZ-Speed: " + flatVelocity.magnitude.ToString("F1") + "\nXYZ-Speed" + rb.velocity.magnitude.ToString("F1");
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void PlayerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //Jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke("ResetJump", jumpCooldown);
        }

        //Start Crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        //Stop Crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, defaultYScale, transform.localScale.z);
        }
    }

    private void MovePlayer()
    {
        //calculate movement direction
        moveDirection =
            orientation.forward * verticalInput +
            orientation.right * horizontalInput; //walk in the direction player looking

        //on slope
        if (OnSlope() &&
            !exitingSlope) // add more force if only player is exactly on slope (It is required because player should be jump on a slope, otherwise he can't)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        //on ground
        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }

        //in air
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        //turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        //limiting speed on slope
        if (OnSlope() &&
            !exitingSlope) // limit only if player is exactly on slope (It required because player should be jump on a slope)
        {
            if (rb.velocity.magnitude > moveSpeed)
            {
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }
        }

        //limiting speed on ground (This is for, I don't want to limit jump velocity)
        else
        {
            Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            //limit velocity if needed
            if (flatVelocity.magnitude > moveSpeed)
            {
                Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        //reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    private void StateHandler()
    {
        //Wallrunning State
        if (wallRunning)
        {
            state = MovementState.wallrunning;
            desiredMoveSpeed = wallRunSpeed;
            
            stateText.text = "WallRunning";
        }

        //Sliding State
        if (sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
            {
                desiredMoveSpeed = slideSpeed;
                stateText.text = "SlidingOnSlope";
            }
            else
            {
                desiredMoveSpeed = sprintSpeed;
                stateText.text = "Sliding";
            }
        }

        //Crouching State
        else if (grounded && Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;

            stateText.text = "Crouching";
        }

        //Sprinting State
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;

            stateText.text = "Sprinting";
        }
        //Walk State
        else if (grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;

            stateText.text = "Walking";
        }
        //On Air State
        else
        {
            state = MovementState.onAir;

            stateText.text = "Air";
        }

        //check if desiredMoveSpeed has changed instantly
        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 8f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeedToDesiredSpeed());
        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeedToDesiredSpeed()
    {
        // smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);

            //For UI/Debug
            onSlopeAndAngelText.text = "Angle Between Ground: " + angle.ToString("F1") + "\nOnSlope: " +
                                       (angle < maxSlopeAngle && angle != 0);


            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized; //direction relative to slope
    }
}