using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour, InputMaster.IWalkingActions
{
    [Header("Player Movement Settings")]
    // maximum speed of the player on the xz plane
    public float inertia = 3.0f;
    public float jumpPower = 5.0f;

    public float airbourneSpeed = 1.0f;
    public float movementSpeed = 0.1f; // the approximant speed the root motion of the animations moves the character

    [Header("Player Physics Settings")]
    public float playerGravity = -9.8f;

    [Header("Attached Game Objects")]
    public InputMaster inputActions;
    public CameraController cameraController;
    public Animator animator;
    public Transform lookFrom;

    private CharacterController controller;
    private InputMaster controls;

    private float forwardAmount;
    private float turnAmount;
    private float jumpLeg;

    private bool land = true;

    private Vector2 targetVelocity; // local space target velocity according to inputs
    private Vector3 velocity; // local space velocity of the player

    private Vector2 currentMove; // vector containing the direction of player input, if any [(0.0, 0.0, 1.0) would be "w"]
    private Vector3 lastFrameVelocity;
    private Vector3 deltaPosition;

    private static float epsilon = 0.00001f;

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (controller.isGrounded)
        {
            jumpLeg = (animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y >
            animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y ? -1.0f : 1.0f) * forwardAmount;

            deltaPosition.y = jumpPower * Time.deltaTime;
            velocity = lastFrameVelocity;
            velocity.y = jumpPower;
            land = false; 
        }
    }
    public void OnMove(InputAction.CallbackContext ctx)
    {
        currentMove = ctx.ReadValue<Vector2>();
        ProcessInput(currentMove);
    }   

    public void OnLook(InputAction.CallbackContext ctx)
    {
        Vector2 input = .05f*ctx.ReadValue<Vector2>();
        cameraController.Look(input);

        if (currentMove.sqrMagnitude > epsilon)
        {
            ProcessInput(currentMove);
        }
    }

    private void ProcessInput(Vector2 currentMove)
    {
        Vector3 transformVelocity = new Vector3(currentMove.x, 0.0f, currentMove.y);
        transformVelocity = Quaternion.Euler(0.0f, lookFrom.rotation.eulerAngles.y, 0.0f) * transformVelocity;
        targetVelocity.x = transformVelocity.x;
        targetVelocity.y = transformVelocity.z;
    }

    public void OnEnable()
    {
        if (controls == null)
        {
            controls = new InputMaster();
            controls.Walking.SetCallbacks(this);
        }
        controls.Walking.Enable();
    }

    public void OnDisable()
    {
        controls.Walking.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        animator.SetBool("Crouch", false);
    }
    
    void FixedUpdate()
    { 
    }

    void Update()
    {
        calculateInertia();
        updateAnimations();

        controller.Move(deltaPosition);
        cameraController.Move();

        velocity.y += playerGravity * Time.deltaTime;
        velocity.y = (controller.isGrounded && velocity.y < 0.0f) ? 2.0f * playerGravity * Time.deltaTime : velocity.y;

        if (!land && controller.isGrounded)
        {
            land = true;
            velocity = lastFrameVelocity*movementSpeed;
        }
    }

    public void animatorMoved()
    {
        if (controller.isGrounded)
        {
            deltaPosition = animator.deltaPosition;
            lastFrameVelocity = deltaPosition / Time.deltaTime;
            float deltaYRotation = animator.deltaRotation.eulerAngles.y;

            Vector3 rotation = transform.eulerAngles;
            rotation.y += deltaYRotation;
            transform.eulerAngles = rotation;

            deltaPosition.y = playerGravity*Time.deltaTime;
        }
        else
        {
            deltaPosition = velocity * Time.deltaTime;
        }
    }

    private void calculateInertia()
    {
        if (controller.isGrounded)
        {
            Vector2 difference = targetVelocity - new Vector2(velocity.x, velocity.z);
            float differenceValue = difference.magnitude;
            difference = difference / differenceValue;

            if (differenceValue <= inertia * Time.deltaTime)
            {
                velocity.x = targetVelocity.x;
                velocity.z = targetVelocity.y;
            }
            else
            {
                velocity.x += difference.x * inertia * Time.deltaTime;
                velocity.z += difference.y * inertia * Time.deltaTime;
            }
        }
        else
        {
            Vector3 step = new Vector3(targetVelocity.x, 0.0f, targetVelocity.y);
            velocity += step * airbourneSpeed * Time.deltaTime;
        }
    }

    private void updateAnimations()
    {
        Vector3 step = velocity;
        step = transform.InverseTransformDirection(step);
        step = Vector3.ProjectOnPlane(step, transform.up);
        turnAmount = Mathf.Atan2(step.x, step.z);
        forwardAmount = step.z;

        animator.SetFloat("Forward", forwardAmount, 0.1f, Time.deltaTime);
        animator.SetFloat("Turn", turnAmount, 0.1f, Time.deltaTime);
        animator.SetBool("OnGround", controller.isGrounded);

        if (!controller.isGrounded)
        {
            animator.SetFloat("Jump", -2.0f * velocity.y/playerGravity);
        }
        animator.SetFloat("JumpLeg", jumpLeg);
    }
}
