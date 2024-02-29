using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BodyInfo;
using static Limb;

public class BodyController : MonoBehaviour
{
    public BodyInfo so_initialBodyStats;

    public InputController input;

    CoolingModel cooling;
    private Coroutine decrementCoroutine = null;
    public GameObject coolingGauge;
    Vector3 coolingGaugeScaleCache;

    HeadModel head;

    LegsModel legs;
    SensorsModel sensors;
    WeaponsModel weapons;
    public GunSelector guns;
    public GameObject weapon1gauge;
    public GameObject weapon2gauge;
    public GameObject weapon3gauge;
    //public GunSelectorTest gun1;
    //public GunSelectorTest gun2;
    //public GunSelectorTest gun3;

    List<SystemModel> systemControllers;
    public Rigidbody rb;
    public Rigidbody ragdollCore;
    public GameObject headObject;
    public GameObject aimCam;

    RaycastHit hit;
    public LayerMask aimMask;
    public Transform weaponAimPoint;
    public Transform torsoAimPoint;

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

        coolingGaugeScaleCache = coolingGauge.transform.localScale;
    }

    List<SystemModel> InitSystems()
    {
        List<SystemModel> models = new List<SystemModel>();
        for(int i = 0; i < so_initialBodyStats.rawSystems.Length; i++)
        {
            BodyInfo.systemID sys = so_initialBodyStats.rawSystems[i];
            switch (sys)
            {
                case BodyInfo.systemID.Cooling:
                    cooling = new CoolingModel(so_initialBodyStats.rawSystemStartLevels[i], rb);
                    models.Add(cooling);
                    Debug.Log("Cooling added");
                    break;
                case BodyInfo.systemID.Legs:
                    legs = new LegsModel(so_initialBodyStats.rawSystemStartLevels[i], rb);
                    models.Add(legs);
                    Debug.Log("Legs added");
                    break;
                case BodyInfo.systemID.Sensors:
                    sensors = new SensorsModel(so_initialBodyStats.rawSystemStartLevels[i], headObject);
                    models.Add(sensors);
                    Debug.Log("Sensors added");
                    break;
                case BodyInfo.systemID.Weapons:
                    weapons = new WeaponsModel(so_initialBodyStats.rawSystemStartLevels[i], guns);
                    models.Add(weapons);
                    Debug.Log("Weapons added");
                    break;
                case BodyInfo.systemID.Head:
                    head = new HeadModel(so_initialBodyStats.rawSystemStartLevels[i]);
                    models.Add(head);
                    Debug.Log("Head added");
                    break;
                default:
                    break;
            }
        }

        weapons.RaiseFiredWeapon += cooling.IncreaseHeat;
        cooling.RaiseIncreasedHeat += StopCooling;
        cooling.RaiseOverheated += weapons.OnCoolingSystemOverheat;
        cooling.RaiseCooledDownFromOverheat += weapons.OnCoolingSystemCooledOff;

        head.RaiseDeath += Die;

        return models;
    }

    SystemModel GetSystem(BodyInfo.systemID sysID)
    {
        return systemControllers.Find(s => s.name == sysID);
    }

    public void HandleDamage(DamageInfo i)
    {
        if (cooling.isOverheated)
        {
            DamageSystem(i);
        }
        else
        {
            cooling.IncreaseHeat(this, i.amount);
        }
    }

    public void DamageSystem(DamageInfo i)
    {
        if(i.limb.specificLimb == Limb.LimbID.none)
        {
            GetSystem(i.limb.linkedSystem).Damage(1);
        }
        else
        {
            switch (i.limb.specificLimb)
            {
                case LimbID.leftLeg:
                    legs.damageLeftLeg(1);
                    break;
                case LimbID.rightLeg:
                    legs.damageRightLeg(1);
                    break;
                case LimbID.head:
                    head.Damage((int)i.amount);
                    break;
            }
        }
    }

    public void Die()
    {
        ActiveRagdollController arc = GetComponentInChildren<ActiveRagdollController>();
        arc.enabled = false;

        Debug.Log("Dead!");
        Debug.Log(GetComponentsInChildren<ConfigurableJoint>().Length);
        foreach(ConfigurableJoint j in GetComponentsInChildren<ConfigurableJoint>())
        {
            JointDrive d = new JointDrive();
            d = j.angularXDrive;
            d.positionSpring = 0;
            j.angularXDrive = d;

            d = j.angularYZDrive;
            d.positionSpring = 0;
            j.angularYZDrive = d;

            d = j.slerpDrive;
            d.positionSpring = 0;
            j.slerpDrive = d;
        }

        foreach(Rigidbody r in GetComponentsInChildren<Rigidbody>())
        {
            //r.sleepThreshold = 0.5f;
            r.drag = 0;
            r.angularDrag = 0;
        }
        ragdollCore.isKinematic = false;
        ragdollCore.constraints = RigidbodyConstraints.None;
        ragdollCore.AddForce(new Vector3(0, 0, -1000));
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

    public void FireWeapon1()
    {
        weapons.ExecuteWeapon1();
        //Debug.Log(guns.ActiveGun1.Model.transform.position);
    }

    public void FireWeapon2()
    {
        weapons.ExecuteWeapon2();
        //Debug.Log(guns.ActiveGun2.Model.transform.position);
    }

    public void FireWeapon3()
    {
        weapons.ExecuteWeapon3();
        //Debug.Log(guns.ActiveGun3.Model.transform.position);
    }

    public void CycleWeaponPowerAllocation()
    {
        weapons.CycleToNextPowerAllocationDictionary();
            
        // TODO Temporary weapon gauge visual
        weapon1gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[0]);
        weapon2gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[1]);
        weapon3gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[2]);

        //weapons.PrintPowerAllocation(weapons.GetCurrentPowerAllocation());
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

    private void GetAimPoint()
    {
        Vector3 torso = aimCam.transform.position + 20 * aimCam.transform.forward;
        if (Physics.Raycast(aimCam.transform.position, aimCam.transform.forward, out hit, Mathf.Infinity, aimMask))
        {
            //Debug.DrawRay(head.transform.position, head.transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            //Debug.Log(hit.distance);
            //weaponAimPoint.position = Vector3.Lerp(weaponAimPoint.position, hit.point, 0.2f);
            weaponAimPoint.position = hit.point;
        }
        else
        {
           //weaponAimPoint.position = Vector3.Lerp(weaponAimPoint.position, torso, 0.2f);
           weaponAimPoint.position = torso;
        }
        torsoAimPoint.position = torso;
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

        if (input.getFire1()) FireWeapon1();
        if (input.getFire2()) FireWeapon2();
        if (input.getFire3()) FireWeapon3();

        if (input.getScroll()) CycleWeaponPowerAllocation();
    }

    private void doCooling()
    {
        if(rb.velocity.magnitude > 0.05f)
        {
            StopCooling();
        }
        else
        {
            cooling.Cooldown();
        }

        //TODO Temporary heating gauge visual
        coolingGauge.transform.localScale = coolingGaugeScaleCache * Mathf.Clamp(((cooling.currentHeat + 0.01f) / cooling.getMaxHeat()),0, 1f);
    }

    //public void StartCooling()
    //{
    //    if (decrementCoroutine == null)
    //    {
    //        decrementCoroutine = StartCoroutine(cooling.DecreaseHeatCoroutine());
    //    }
    //}

    public void StopCooling()
    {
        cooling.ResetCooldown();
    }

    // Update is called once per frame
    void Update()
    {
        DoRotation();
        GetAimPoint();
    }

    private void FixedUpdate()
    {
        ExecutePhysicsBasedInputs();
        legs.DoMoveDeacceleration();
        doCooling();
    }
}
