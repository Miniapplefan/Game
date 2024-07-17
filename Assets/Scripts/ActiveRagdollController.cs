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
	public Rigidbody RagdollLeftFootRb;
	public Rigidbody RagdollRightFootRb;
	public Rigidbody RagdollRightArmRb;
	public Rigidbody RagdollSpineLowerRb;
	public AnimationCurve uprightTorqueFunction;
	public float uprightTorque = 10000;
	public float rotationTorque = 500;
	public Vector3 TargetDirection { get; set; }
	private Quaternion _targetRotation;
	public Transform AnimatedRightArm;
	public Transform AnimatedRightWeapon;
	public Transform target;

	public ProceduralAnimation proceduralAnimation;

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
		//RagdollLeftFoot = AnimatedLeftFoot;
		//RagdollRightFoot = AnimatedRightFoot;
		UpdateTargetRotation();
		ApplyUprightTorque();
		// if (proceduralAnimation.leftFootTargetRig.localPosition.y > -0.25f)
		// {
		// 	RagdollLeftFootRb.isKinematic = false;
		// }
		// else
		// {
		// 	RagdollLeftFootRb.isKinematic = true;
		// }

		// if (proceduralAnimation.rightFootTargetRig.localPosition.y > -0.25f)
		// {
		// 	RagdollRightFootRb.isKinematic = false;
		// }
		// else
		// {
		// 	RagdollRightFootRb.isKinematic = true;
		// }
	}

	private void ApplyUprightTorque()
	{
		var balancePercent = Vector3.Angle(RagdollSpineLowerRb.transform.up,
														 Vector3.up) / 180;
		balancePercent = uprightTorqueFunction.Evaluate(balancePercent);
		var rot = Quaternion.FromToRotation(RagdollSpineLowerRb.transform.up,
											 Vector3.up).normalized;

		RagdollSpineLowerRb.AddTorque(new Vector3(rot.x, rot.y, rot.z)
													* uprightTorque * balancePercent);

		var directionAnglePercent = Vector3.SignedAngle(RagdollSpineLowerRb.transform.forward,
							TargetDirection, Vector3.up) / 180;
		RagdollSpineLowerRb.AddRelativeTorque(0, directionAnglePercent * rotationTorque, 0);
	}

	private void UpdateTargetRotation()
	{
		if (TargetDirection != Vector3.zero)
			_targetRotation = Quaternion.LookRotation(TargetDirection, Vector3.up);
		else
			_targetRotation = Quaternion.identity;
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
