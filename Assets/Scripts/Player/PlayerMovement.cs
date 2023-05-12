using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Transform playerCamera;
    [SerializeField] bool cursorLock = true;
    [SerializeField] [Range(0.0f, 0.5f)] float mouseSmoothTime = 0.03f;
    [SerializeField] float mouseSensitivity;
    [SerializeField] float moveSpeed = 6.0f;
    [SerializeField] [Range(0.0f, 0.5f)] float moveSmoothTime = 0.3f;
    public float gravity = -10f;
    [SerializeField] Transform groundCheck; 
    [SerializeField] LayerMask ground;

    public float jumpHeight = 6f;
    float velocityY;
    bool isGrounded;

    [SerializeField] float cameraCap;
    Vector2 currentMouseDelta;
    Vector2 currentMouseDeltaVelocity;

    Vector2 targetDir;
    Vector2 targetDirVelocity;

    CharacterController controller;
    Cloning cloningScript;
    Recorder recorder;

    Vector2 cameraInputValue;
    Vector2 moveInputValue;

    float cameraRotY;

    public InputActionAsset actions;
    public InputAction cameraInput, jumpButton, moveInput, gravButton;

    public bool canGravJump;

    public bool canJetpack;
    private Oxygen oxy;
    public float jetCost;
    public GameObject waterParticle;

    public bool stopMove;

    void Start()
    {
        Application.targetFrameRate = 200;

        oxy = GetComponent<Oxygen>();
        cameraInput = actions.FindActionMap("movement", true).FindAction("camera", true);
        jumpButton = actions.FindActionMap("movement", true).FindAction("jump", true);
        gravButton = actions.FindActionMap("movement", true).FindAction("gravity", true);
        moveInput = actions.FindActionMap("movement", true).FindAction("move", true);
        actions.FindActionMap("movement").Enable();

        controller = GetComponent<CharacterController>();
        recorder = GetComponent<Recorder>();
        cloningScript = GetComponent<Cloning>();

        if (cursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
        }

        moveInputValue = new();

        Vector3 rot = transform.rotation.eulerAngles;
        cameraRotY = rot.y;

        stopMove = false;
    }

    void Update()
    {
        if (stopMove) return;
        UpdateMouse();
        UpdateMove();
    }

    void UpdateMouse()
    {
        cameraInputValue = cameraInput.ReadValue<Vector2>();

        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, cameraInputValue, ref currentMouseDeltaVelocity, mouseSmoothTime);

        transform.Rotate(Vector3.up, currentMouseDelta.x * mouseSensitivity);

        cameraRotY += currentMouseDelta.y * mouseSensitivity;
        cameraRotY = Mathf.Clamp(cameraRotY, -cameraCap, cameraCap);
        playerCamera.localEulerAngles = (-Vector3.right * cameraRotY);
    }

    void UpdateMove()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.35f, ground);

        Vector2 newMoveInputValue;

        newMoveInputValue = moveInput.ReadValue<Vector2>();
        newMoveInputValue.Normalize();

        moveInputValue = newMoveInputValue;

        targetDir = Vector2.SmoothDamp(targetDir, moveInputValue, ref targetDirVelocity, moveSmoothTime);

        velocityY += gravity * Time.deltaTime;
        Vector3 velocity = (transform.forward * targetDir.y + transform.right * targetDir.x) * moveSpeed + Vector3.up * velocityY;
        controller.Move(velocity * Time.deltaTime);

        // JUMP
        if (jumpButton.WasPressedThisFrame() && (isGrounded || (!isGrounded && canJetpack)))
        {
            int mul = 1;
            if (!isGrounded)
            {
                mul = 3;
                oxy.oxygenValue -= jetCost;
                JetPackParticles();
            }
            velocityY = Jump(jumpHeight * mul);
        }
        // GRAVITY Switch
        if (gravButton.WasPressedThisFrame() && canGravJump && isGrounded)
        {
            gravity *= -1;

            transform.Rotate(Vector3.right, 180f);
            transform.Rotate(Vector3.up, 180f);
            cameraRotY *= -1;
            playerCamera.localEulerAngles = (-Vector3.right * cameraRotY);

            velocityY = 0f;
        }
    }

    public float Jump(float height)
    {
        float velocityY = Mathf.Sqrt(height * Mathf.Abs(gravity)) * -Mathf.Sign(gravity);
        return velocityY;
    }

    private void JetPackParticles()
    {
        for (int i = 0; i < 30; i++)
        {
            Instantiate(waterParticle, transform.position, Quaternion.identity);
        }
    }
}