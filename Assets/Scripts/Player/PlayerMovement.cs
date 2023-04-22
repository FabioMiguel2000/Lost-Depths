using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// These videos take long to make so I hope this helps you out and if you want to help me out you can by leaving a like and subscribe, thanks!

public class PlayerMovement : MonoBehaviour
{
    public Transform playerCamera;
    [SerializeField] [Range(0.0f, 0.5f)] float mouseSmoothTime = 0.03f;
    [SerializeField] bool cursorLock = true;
    [SerializeField] float mouseSensitivity = 3.5f;
    [SerializeField] float moveSpeed = 6.0f;
    [SerializeField] [Range(0.0f, 0.5f)] float moveSmoothTime = 0.3f;
    public float gravity = -30f;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask ground;

    public float jumpHeight = 6f;
    float velocityY;
    bool isGrounded;

    float cameraCap;
    Vector2 currentMouseDelta;
    Vector2 currentMouseDeltaVelocity;

    Vector2 currentDir;
    Vector2 currentDirVelocity;

    CharacterController controller;
    Cloning cloningScript;
    Recorder recorder;

    // Clone Playing
    Vector2 targetDir;
    Vector2 targetMouseDelta;
    int moveDirUpdateIndex;
    int cameraUpdateIndex;
    int jumpIndex;
    // Clone Recording
    bool hasRecordedMoveDirUpdate;
    bool hasRecordedCameraUpdate;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        recorder = GetComponent<Recorder>();
        cloningScript = GetComponent<Cloning>();


        if (!cloningScript.isClone) {
            // Find Main Camera
            for (var i = gameObject.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = gameObject.transform.GetChild(i).gameObject;
                if (child.CompareTag("MainCamera"))
                {
                    playerCamera = child.transform;
                }
            }
        }

        if (cursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
        }

        // Clone Playing
        targetDir = new();
        targetMouseDelta = new();
        moveDirUpdateIndex = 0;
        cameraUpdateIndex = 0;
        jumpIndex = 0;
        // Clone Recording
        hasRecordedMoveDirUpdate = false;
    }

    void Update()
    {
        UpdateMouse();
        UpdateMove();
    }

    void UpdateMouse()
    {
        Vector2 newTargetMouseDelta;

        if (!cloningScript.isClone)
        {
            newTargetMouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            if (recorder.isRecording && (newTargetMouseDelta != targetMouseDelta || !hasRecordedCameraUpdate))  // is different OR is the first recording))
            {
                recorder.Push(Recorder.EventType.CameraUpdate, Time.time - recorder.GetRecordingStartTime(), newTargetMouseDelta);
                hasRecordedCameraUpdate = true;
            }
            else if (!recorder.isRecording) hasRecordedCameraUpdate = false;

            targetMouseDelta = newTargetMouseDelta;
        }
        else
        {
            // Get Playback Value
            Tuple<Recorder.EventType, float, Vector3> tuple = cloningScript.recorder.GetEvent(cameraUpdateIndex);
            while (tuple != null && tuple.Item2 <= Time.time - cloningScript.recorder.GetPlayStartTime())
            {
                if (tuple.Item1 == Recorder.EventType.CameraUpdate)
                {
                    targetMouseDelta = tuple.Item3;
                }
                cameraUpdateIndex++;
                tuple = cloningScript.recorder.GetEvent(cameraUpdateIndex);
            }
        }

        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, mouseSmoothTime);
        cameraCap -= currentMouseDelta.y * mouseSensitivity;
        cameraCap = Mathf.Clamp(cameraCap, -90.0f, 90.0f);
        playerCamera.localEulerAngles = Vector3.right * cameraCap;
        var rotationValue = Vector3.up * currentMouseDelta.x * mouseSensitivity;
        transform.Rotate(rotationValue);
    }

    void UpdateMove()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.35f, ground);

        Vector2 newTargetDir;

        if (!cloningScript.isClone)
        {
            newTargetDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            newTargetDir.Normalize();

            if (recorder.isRecording && (!newTargetDir.Equals(targetDir) || !hasRecordedMoveDirUpdate))
            {
                recorder.Push(Recorder.EventType.MoveDirUpdate, Time.time - recorder.GetRecordingStartTime(), newTargetDir);
                hasRecordedMoveDirUpdate = true;
            }
            else if (!recorder.isRecording) hasRecordedMoveDirUpdate = false;

            targetDir = newTargetDir;
        }
        else
        {
            // Get Playback Value
            Tuple<Recorder.EventType, float, Vector3> tuple = cloningScript.recorder.GetEvent(moveDirUpdateIndex);
            if (tuple != null && tuple.Item2 <= Time.time - cloningScript.recorder.GetPlayStartTime())
            {
                if (tuple.Item1 == Recorder.EventType.MoveDirUpdate)
                {
                    targetDir = tuple.Item3;
                }
                moveDirUpdateIndex++;
                //tuple = cloningScript.recorder.GetEvent(moveDirUpdateIndex);
            }
        }

        currentDir = Vector2.SmoothDamp(currentDir, targetDir, ref currentDirVelocity, moveSmoothTime);

        if (!isGrounded)
            velocityY += gravity * 2f * Time.deltaTime;

        Vector3 velocity = (transform.forward * currentDir.y + transform.right * currentDir.x) * moveSpeed + Vector3.up * velocityY;

        controller.Move(velocity * Time.deltaTime);


        // JUMP
        if (isGrounded)
        {
            if (!cloningScript.isClone && Input.GetButtonDown("Jump"))
            {
                velocityY = Jump();
                if (recorder.isRecording)
                {
                    recorder.Push(Recorder.EventType.Jump, Time.time - recorder.GetRecordingStartTime());
                }
            }
            else if (cloningScript.isClone)
            {
                // Get Playback Value
                Tuple<Recorder.EventType, float, Vector3> tuple2 = cloningScript.recorder.GetEvent(jumpIndex);
                if (tuple2 != null && tuple2.Item2 <= Time.time - cloningScript.recorder.GetPlayStartTime())
                {
                    if (tuple2.Item1 == Recorder.EventType.Jump)
                    {
                        velocityY = Jump();
                    }
                    jumpIndex++;
                    //tuple2 = cloningScript.recorder.GetEvent(jumpIndex);
                }
            }
        }

        //if (isGrounded! && controller.velocity.y < -1f)
        //{
        //    velocityY = -8f;
        //}

        //if (isGrounded && controller.velocity.y < -1f)
        //{
        //    velocityY = 0f;
        //}
    }

    public float Jump()
    {
        float velocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);

        return velocityY;
    }

    public void ResetPlayIndexes()
    {
        moveDirUpdateIndex = 0;
        cameraUpdateIndex = 0;
        jumpIndex = 0;
    }
}