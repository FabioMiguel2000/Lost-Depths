using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Recorder : MonoBehaviour
{
    public enum ActionType { Move, MoveCamera, Jump, StopRecording };

    public bool isRecording;
    bool isPlaying;
    Vector3 startingPosition;
    GameObject startingCamera;
    float recordingStartTime;
    float playStartTime;

    Cloning cloningScript;
    GameObject clone;
    public GameObject cube;
    CharacterController cloneController;
    int playIndex;
    List<Tuple<ActionType, float, Vector3>> actionsArray;

    public InputActionAsset actions;
    public InputAction cloneInput;

    // Start is called before the first frame update
    void Start()
    {
        cloneInput = actions.FindActionMap("movement", true).FindAction("clone", true);
        actions.FindActionMap("movement").Enable();
        isRecording = false;
        actionsArray = new List<Tuple<ActionType, float, Vector3>>();

        cloningScript = GetComponent<Cloning>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlaying)
        {
            Tuple<ActionType, float, Vector3> tuple = actionsArray[playIndex];

            while (tuple.Item2 - recordingStartTime <= Time.time - playStartTime)
            {
                PlayerMovement pm = clone.GetComponent<PlayerMovement>();
                switch (tuple.Item1)
                {
                    case ActionType.Move:
                        cloneController.Move(tuple.Item3);
                        break;
                    case ActionType.MoveCamera:
                        pm.transform.Rotate(tuple.Item3);
                        break;
                    case ActionType.Jump:
                        pm.SetVelocityY(pm.Jump(pm.jumpHeight, pm.gravity));
                        Debug.Log("Jumping");
                        break;
                    case ActionType.StopRecording:
                        isPlaying = false;
                        Destroy(clone);
                        break;
                }
                playIndex++;
                if (playIndex < actionsArray.Count)
                    tuple = actionsArray[playIndex];
                else break;
            }
        }
        else
        {
            if ((cloneInput.ReadValue<float>() > 0) && !cloningScript.isClone)
            {
                StartRecording();
            }
            else if ((cloneInput.ReadValue<float>() > 0) && !cloningScript.isClone)
            {
                StopRecording();
            }

            if (Input.GetKeyDown(KeyCode.Q) && !cloningScript.isClone)
            {
                Play();
            }
        }
    }

    public void StartRecording()
    {
        Debug.Log("Recording Started");

        isRecording = true;
        recordingStartTime = Time.time;

        // Store starting position, and facing direction (XZ only) and gravity modifier (1 or -1)
        startingPosition = gameObject.transform.position;

        // Find startingCamera
        for (var i = gameObject.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child.layer == LayerMask.NameToLayer("Cameras"))
            {
                if (child.name == "Starting Camera")
                {
                    Destroy(child);
                }
                else {
                    // child is startingCamera
                    var oldStartingCamera = startingCamera;
                    Destroy(oldStartingCamera);

                    startingCamera = Instantiate(child, gameObject.transform);
                    startingCamera.name = "Starting Camera";
                    startingCamera.tag = "Untagged";
                    startingCamera.GetComponent<Camera>().enabled = false;
                    startingCamera.transform.parent = cube.transform;
                }
            }
        }
        // More ToDo

        // Reset actionsArray
        actionsArray = new List<Tuple<ActionType, float, Vector3>>();
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        Debug.Log("Recording Stopped");

        isRecording = false;

        Push(ActionType.StopRecording, Time.time);

        // Destroy Player and instantiate them again at the starting position
        //GameObject player = Instantiate(gameObject, startingPosition, Quaternion.identity);
        //player.GetComponent<Cloning>().isClone = false;
        //Destroy(gameObject);

        Debug.Log(actionsArray.Count);
    }

    public void Play()
    {
        if (isRecording || isPlaying) return;

        Debug.Log("Size of array: " + actionsArray.Count);
        if (actionsArray.Count < 1)
        {
            Debug.Log("No actions to play");
            return;
        }

        isPlaying = true;
        playIndex = 0;

        playStartTime = Time.time;

        clone = Instantiate(gameObject, startingPosition, Quaternion.identity);
        clone.GetComponent<Cloning>().InitClone(startingCamera);
        cloneController = clone.GetComponent<CharacterController>();
    }

    public void Push(ActionType actionType, float timestamp)
    {
        actionsArray.Add(new Tuple<ActionType, float, Vector3>(actionType, timestamp, new Vector3(0,0,0)) );
    }
    public void Push(ActionType actionType, float timestamp, Vector3 motion)
    {
        actionsArray.Add(new Tuple<ActionType, float, Vector3>(actionType, timestamp, motion));
    }
}
