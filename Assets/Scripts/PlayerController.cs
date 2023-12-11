using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, InputController
{
    public KeyCode forwardKey;
    public KeyCode backwardKey;
    public KeyCode leftKey;
    public KeyCode rightKey;

    private bool pressingForward;
    private bool pressingBackward;
    private bool pressingLeft;
    private bool pressingRight;

    public float sensitivity;
    private float mouseXrotation;
    private float mouseYrotation;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool getForward()
    {
        return pressingForward;
    }

    public bool getBackward()
    {
        return pressingBackward;
    }

    public bool getLeft()
    {
        return pressingLeft;
    }

    public bool getRight()
    {
        return pressingRight;
    }

    public Vector2 getHeadRotation()
    {
        mouseYrotation = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime;
        mouseXrotation = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime;

        //if(Input.GetAxis("Mouse X") != 0)
        //{
        //    Debug.Log("mouse input registered");
        //}

        mouseXrotation = Mathf.Clamp(mouseXrotation, -90, 90);

        return new Vector2(mouseXrotation, mouseYrotation);
    }

    // Update is called once per frame
    void Update()
    {
        pressingForward = Input.GetKey(forwardKey);
        pressingBackward = Input.GetKey(backwardKey);
        pressingLeft = Input.GetKey(leftKey);
        pressingRight = Input.GetKey(rightKey);
    }
}
