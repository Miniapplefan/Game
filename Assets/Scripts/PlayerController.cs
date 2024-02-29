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

    public float scrollSensitivity = 0.1f;

    public bool pressingFire1;
    public bool pressingFire2;
    public bool pressingFire3;


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

    public bool getFire1()
    {
        return pressingFire1;
    }

    public bool getFire2()
    {
        return pressingFire2;
    }

    public bool getFire3()
    {
        return pressingFire3;
    }

    public bool getScroll()
    {
        // Check if mouse wheel is scrolled
        float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");

        // If scrollWheelInput is not within the sensitivity threshold, return
        if (Mathf.Abs(scrollWheelInput) < scrollSensitivity)
        {
            return false;
        }

        // Set didScroll to true
        return true;
    }

    // Update is called once per frame
    void Update()
    {
        pressingForward = Input.GetKey(forwardKey);
        pressingBackward = Input.GetKey(backwardKey);
        pressingLeft = Input.GetKey(leftKey);
        pressingRight = Input.GetKey(rightKey);

        pressingFire1 = Input.GetMouseButton(0);
        pressingFire2 = Input.GetMouseButton(1);
        pressingFire3 = Input.GetMouseButton(2);
    }
}
