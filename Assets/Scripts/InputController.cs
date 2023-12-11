using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface InputController
{
    public bool getForward();
    public bool getBackward();
    public bool getLeft();
    public bool getRight();

    public Vector2 getHeadRotation();
}
