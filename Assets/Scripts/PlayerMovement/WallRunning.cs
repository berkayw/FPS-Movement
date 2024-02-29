using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class WallRunning : MonoBehaviour
{
    [Header("Wallrunning")] public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    public float wallRunForce;
    public float wallClimbSpeed;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Input")] 
    public KeyCode upwardsWallRunKey = KeyCode.LeftShift;
    public KeyCode downwardsWallRunKey = KeyCode.LeftControl;
    private bool upwardsWallRunning;
    private bool downwardsWallRunning;
 
    
    private float horizontalInput;
    private float verticalInput;

    [Header("WallDetection")] public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("References")] public Transform orientation;
    private PlayerMovement pm;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        CheckForWall();
        WallRunningHandler();
    }

    private void FixedUpdate()
    {
        if (pm.wallRunning)
        {
            WallRunningMovement();
        }
    }

    private void CheckForWall()
    {
        //Check Left and Right Walls and Store it
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance,whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance,whatIsWall);
    }

    private bool AboveGround()
    {
        //Return true if player is above ground (raycast = false)
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void WallRunningHandler()
    {
        //Inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        upwardsWallRunning = Input.GetKey(upwardsWallRunKey);
        downwardsWallRunning = Input.GetKey(downwardsWallRunKey);
        
        //State 1 / Wallrunning
        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround())
        {
            //Start wallrun
            if (!pm.wallRunning)
            {
                StartWallRun();
            }
        }
        else
        {
            if (pm.wallRunning)
            {
                StopWallRun();
            }
        }
    }

    private void StartWallRun()
    {
        pm.wallRunning = true;
    }

    private void WallRunningMovement()
    {
        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up); //Cross of 2 vectors
        
        //check which direction player facing
        if((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }
        
        //Forward force
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
        
        //upwards-downwards force
        if (upwardsWallRunning)
        {
            rb.velocity = new Vector3(rb.velocity.x, wallClimbSpeed, rb.velocity.z);
        }
        if (downwardsWallRunning)
        {
            rb.velocity = new Vector3(rb.velocity.x, -wallClimbSpeed, rb.velocity.z);
        }
        
        
        //push to wall force
        if(!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
        rb.AddForce(-wallNormal * 100, ForceMode.Force);
    }

    private void StopWallRun()
    {
        pm.wallRunning = false;
        rb.useGravity = true;
    }
    
    
}