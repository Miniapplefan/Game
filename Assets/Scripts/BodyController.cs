using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BodyInfo;

public class BodyController : MonoBehaviour
{
    public BodyInfo so_initialBodyStats;

    public InputController input;

    LegsModel legs;
    SensorsModel sensors;

    List<SystemModel> systemControllers;
    public Rigidbody rb;
    public GameObject head;

    float currentSelfXrotation;
    float currentSelfYrotation;
    float currentXrotationRef;
    float currentYrotationRef;

    // Start is called before the first frame update
    void Start()
    {
        //so_initialBodyStats = (BodyInfo)Resources.Load<ScriptableObject>("PlayerStartBodyInfo");
        systemControllers = InitSystems();
        rb = GetComponent<Rigidbody>();

        //InputController can be either a player or AI. We check if it's a PlayerController and
        //if it isn't we make it an AI
        if (GetComponent<PlayerController>() != null)
        {
            Debug.Log("found player controller");
            input = GetComponent<PlayerController>();
        }
        else
        {
            //TODO put input = GetComponent<AIController>()
            Debug.Log("No controller");
        }
    }

    List<SystemModel> InitSystems()
    {
        List<SystemModel> models = new List<SystemModel>();
        for(int i = 0; i < so_initialBodyStats.rawSystems.Length; i++)
        {
            BodyInfo.systemID sys = so_initialBodyStats.rawSystems[i];
            switch (sys)
            {
                case BodyInfo.systemID.Legs:
                    models.Add(new LegsModel(so_initialBodyStats.rawSystemStartLevels[i], rb));
                    legs = new LegsModel(so_initialBodyStats.rawSystemStartLevels[i], rb);
                    break;
                case BodyInfo.systemID.Sensors:
                    models.Add(new SensorsModel(so_initialBodyStats.rawSystemStartLevels[i], head));
                    sensors = new SensorsModel(so_initialBodyStats.rawSystemStartLevels[i], head);
                    break;
                case BodyInfo.systemID.Weapons:
                    break;
                case BodyInfo.systemID.Shields:
                    break;
                default:
                    break;
            }
        }
        return models;
    }

    SystemModel GetSystem(BodyInfo.systemID sysID)
    {
        return systemControllers.Find(s => s.name == sysID);
    }

    #region Inputs

    public void MoveForward()
    {
        legs.ExecuteForward();
    }

    public void MoveBackward()
    {
        legs.ExecuteBackward();
    }

    public void MoveLeft()
    {
        legs.ExecuteLeft();
    }

    public void MoveRight()
    {
        legs.ExecuteRight();
    }

    private void DoRotation()
    {
        sensors.setHeadRotation(input.getHeadRotation());

        //currentSelfYrotation = Mathf.SmoothDamp(currentSelfYrotation, input.getHeadRotation().y, ref currentYrotationRef, sensors.rotationSmoothDamp);
        //currentSelfXrotation = Mathf.SmoothDamp(currentSelfXrotation, input.getHeadRotation().x, ref currentXrotationRef, sensors.rotationSmoothDamp);

        transform.Rotate(0, input.getHeadRotation().y, 0);
        
        //if (transform.rotation != Quaternion.Euler(0, currentSelfYrotation, 0))
        //{
        //    transform.Rotate(Vector3.up * currentSelfXrotation);
        //}
    }

    #endregion

    private void ExecutePhysicsBasedInputs()
    {
        if (legs.isCurrentVelocityLessThanMax())
        {
            if (input.getForward()) MoveForward();
            if (input.getBackward()) MoveBackward();
            if (input.getLeft()) MoveLeft();
            if (input.getRight()) MoveRight();
        }
    }

    // Update is called once per frame
    void Update()
    {
        DoRotation();
    }

    private void FixedUpdate()
    {
        ExecutePhysicsBasedInputs();
        legs.DoMoveDeacceleration();
    }
}
