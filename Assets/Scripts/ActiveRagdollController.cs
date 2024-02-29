using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public struct BoneJointPair
{
    public Transform bone;
    public ConfigurableJoint joint;
}

public class ActiveRagdollController : MonoBehaviour
{
    public BoneJointPair[] bonesAndJoints;
    private Quaternion[] _initialJointsRotation;
    private Rigidbody[] Rigidbodies;
    public Transform AnimatedRightFoot;
    public Transform AnimatedLeftFoot;
    public Transform RagdollRightFoot;
    public Transform RagdollLeftFoot;
    public Transform RagdollRightArm;
    public Transform RagdollRightWeapon;
    public Transform AnimatedHead;
    public Rigidbody RagdollHead;
    public Rigidbody RagdollRightArmRb;
    public Transform AnimatedRightArm;
    public Transform AnimatedRightWeapon;
    public Transform target;

    // Start is called before the first frame update
    void Start()
    {
        _initialJointsRotation = new Quaternion[bonesAndJoints.Length];
        for (int i = 0; i < bonesAndJoints.Length; i++)
        {
            _initialJointsRotation[i] = bonesAndJoints[i].bone.localRotation;
        }
        //for (int i = 0; i < bonesAndJoints.Length; i++)
        //{
        //    ConfigurableJointExtensions.SetupAsCharacterJoint(bonesAndJoints[i].joint);
        //}
        Rigidbodies = this.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in Rigidbodies)
        {
            rb.solverIterations = 70;
            //rb.solverVelocityIterations = 20;
            //rb.maxAngularVelocity = 20;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        UpdateJointTargets();
        RagdollLeftFoot = AnimatedLeftFoot;
        RagdollRightFoot = AnimatedRightFoot;
    }

    private void LateUpdate()
    {
        if (RagdollRightArmRb.velocity.magnitude < 0.5f)
        {
            float speed = (1f / RagdollRightArmRb.velocity.magnitude);
            Vector3 direction = target.transform.position - RagdollRightArm.transform.position;
            Quaternion toRotation = Quaternion.LookRotation(direction, transform.up);
            //AnimatedRightArm.transform.rotation = toRotation;
            //AnimatedRightWeapon.transform.rotation = toRotation;
            //RagdollRightWeapon.transform.rotation = Quaternion.Lerp(RagdollRightWeapon.transform.rotation, toRotation, speed * Time.deltaTime);
            //RagdollRightArm.transform.rotation = Quaternion.Lerp(RagdollRightArm.transform.rotation, toRotation, speed * Time.deltaTime);
            //RagdollRightArm.transform.rotation = toRotation;
            RagdollRightArm.transform.rotation = Quaternion.Lerp(RagdollRightArm.transform.rotation, toRotation, speed * Time.deltaTime);

            Vector3 d = target.transform.position - RagdollRightWeapon.transform.position;
            Quaternion t = Quaternion.LookRotation(d, transform.right);
            RagdollRightWeapon.transform.rotation = Quaternion.Lerp(RagdollRightWeapon.transform.rotation, t, speed * Time.deltaTime);
            //AnimatedRightWeapon.transform.rotation = t;
            //RagdollRightArm.LookAt(target.transform.position, Vector3.up);
            //AnimatedRightArm.LookAt(target.transform.position, Vector3.up);
            //RagdollRightWeapon.transform.rotation = t;

        }
        //RagdollHead.transform.rotation = AnimatedHead.rotation;
    }

    private void UpdateJointTargets()
    {
        for (int i = 0; i < bonesAndJoints.Length; i++)
        {
            ConfigurableJointExtensions.SetTargetRotationLocal(bonesAndJoints[i].joint, bonesAndJoints[i].bone.localRotation, _initialJointsRotation[i]);
        }
    }
}
