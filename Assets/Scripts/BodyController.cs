using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static BodyInfo;
using static Limb;

public class BodyController : MonoBehaviour
{
	public BodyInfo so_initialBodyStats;

	public BodyState bodyState;

	public InputController input;

	bool isAI = false;

	bool isDead = false;

	CoolingModel cooling;

	HeatContainer heatContainer;
	private Coroutine decrementCoroutine = null;
	public GameObject coolingGauge;
	Vector3 coolingGaugeScaleCache;

	public GameObject taggingGauge;
	Vector3 taggingGaugeScaleCache;

	HeadModel head;

	public LegsModel legs;
	SensorsModel sensors;
	WeaponsModel weapons;
	public Rigidbody weaponRigidbody;
	public GunSelector guns;
	public GameObject weapon1gauge;
	public GameObject weapon2gauge;
	public GameObject weapon3gauge;
	//public GunSelectorTest gun1;
	//public GunSelectorTest gun2;
	//public GunSelectorTest gun3;
	SiphonModel siphon;
	public Transform siphonHead;

	List<SystemModel> systemControllers;
	public Rigidbody rb;
	public Rigidbody ragdollCore;
	public GameObject headObject;
	public GameObject aimCam;

	public ConfigurableJoint upperTorsoJoint;
	public ConfigurableJoint middleTorsoJoint;

	public ConfigurableJoint upperRightArmJoint;

	private JointDrive tempJoint;

	public MultiAimConstraint upperTorsoMac;
	public MultiAimConstraint lowerTorsoMac;

	public Transform taggingTarget;

	RaycastHit hit;
	public LayerMask aimMask;
	public Transform weaponAimPoint;
	public Transform torsoAimPoint;
	private float lastRaycastTime;
	private float raycastInterval = 0.1f; // Adjust this value as needed
	public Collider[] bodyColliders; // Array to hold player's own colliders

	float currentSelfXrotation;
	float currentSelfYrotation;
	float currentXrotationRef;
	float currentYrotationRef;

	// Start is called before the first frame update
	void Start()
	{
		//so_initialBodyStats = (BodyInfo)Resources.Load<ScriptableObject>("PlayerStartBodyInfo");
		systemControllers = InitSystems();
		heatContainer = GetComponent<HeatContainer>();
		heatContainer.InitCoolingModel(cooling);
		SubscribeSystemEvents();
		bodyState.Init(systemControllers, heatContainer);
		rb = GetComponent<Rigidbody>();
		bodyColliders = GetComponentsInChildren<Collider>();
		tempJoint = new JointDrive();

		//InputController can be either a player or AI. We check if it's a PlayerController and
		//if it isn't we make it an AI
		if (GetComponent<PlayerController>() != null)
		{
			Debug.Log("found player controller");
			input = GetComponent<PlayerController>();
		}
		else
		{
			input = GetComponent<AIController>();
			isAI = true;
		}

		coolingGaugeScaleCache = coolingGauge.transform.localScale;
		taggingGaugeScaleCache = taggingGauge.transform.localScale;
	}

	List<SystemModel> InitSystems()
	{
		List<SystemModel> models = new List<SystemModel>();
		for (int i = 0; i < so_initialBodyStats.rawSystems.Length; i++)
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
					weapons = new WeaponsModel(so_initialBodyStats.rawSystemStartLevels[i], guns, weaponRigidbody);
					models.Add(weapons);
					Debug.Log("Weapons added");
					break;
				case BodyInfo.systemID.Head:
					head = new HeadModel(so_initialBodyStats.rawSystemStartLevels[i]);
					models.Add(head);
					Debug.Log("Head added");
					break;
				case BodyInfo.systemID.Siphon:
					siphon = new SiphonModel(so_initialBodyStats.rawSystemStartLevels[i], siphonHead);
					models.Add(siphon);
					Debug.Log("Siphon added");
					break;
				default:
					break;
			}
		}

		weapons.CycleToNextPowerAllocationDictionary();

		return models;
	}

	void SubscribeSystemEvents()
	{
		//weapons.RaiseFiredWeapon += heatContainer.IncreaseHeat;

		heatContainer.OnOverheated += () => cooling.SetOverheated(true);

		//cooling.RaiseIncreasedHeat += StopCooling;

		heatContainer.OnOverheated += weapons.OnCoolingSystemOverheat;
		//cooling.RaiseOverheated += weapons.OnCoolingSystemOverheat;
		cooling.RaiseCooledDownFromOverheat += weapons.OnCoolingSystemCooledOff;

		heatContainer.OnOverheated += legs.OnCoolingSystemOverheat;
		//cooling.RaiseOverheated += legs.OnCoolingSystemOverheat;
		cooling.RaiseCooledDownFromOverheat += legs.OnCoolingSystemCooledOff;


		head.RaiseDeath += Die;
	}

	SystemModel GetSystem(BodyInfo.systemID sysID)
	{
		return systemControllers.Find(s => s.name == sysID);
	}

	public void HandleDamage(DamageInfo i)
	{
		legs.HandleTagging(i.limb, i.impactForce);
		weapons.HandleDisruption(i.limb);
		if (cooling.isOverheated)
		{
			DamageSystem(i);
		}
		else
		{
			heatContainer.IncreaseHeat(this, i.amount);
			//cooling.IncreaseHeat(this, i.amount);
		}
	}

	public void DamageSystem(DamageInfo i)
	{
		if (i.limb.specificLimb == Limb.LimbID.none)
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
		isDead = true;

		ActiveRagdollController arc = GetComponentInChildren<ActiveRagdollController>();
		arc.enabled = false;

		Debug.Log("Dead!");
		Debug.Log(GetComponentsInChildren<ConfigurableJoint>().Length);
		foreach (ConfigurableJoint j in GetComponentsInChildren<ConfigurableJoint>())
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

		foreach (Rigidbody r in GetComponentsInChildren<Rigidbody>())
		{
			//r.sleepThreshold = 0.5f;
			r.drag = 0;
			r.angularDrag = 0;
		}
		ragdollCore.isKinematic = false;
		ragdollCore.constraints = RigidbodyConstraints.None;
		//ragdollCore.AddForce(new Vector3(0, 0, -1000));
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

		// TODO This is just to debug the AI cycling power allocations 
		weapon1gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[0]);
		weapon2gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[1]);
		weapon3gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[2]);
	}

	public void FireWeapon2()
	{
		weapons.ExecuteWeapon2();
		//Debug.Log(guns.ActiveGun2.Model.transform.position);

		// TODO This is just to debug the AI cycling power allocations 
		weapon1gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[0]);
		weapon2gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[1]);
		weapon3gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[2]);
	}

	public void FireWeapon3()
	{
		weapons.ExecuteWeapon3();
		//Debug.Log(guns.ActiveGun3.Model.transform.position);

		// TODO This is just to debug the AI cycling power allocations 
		weapon1gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[0]);
		weapon2gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[1]);
		weapon3gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[2]);
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
		if (Time.time - lastRaycastTime >= raycastInterval && rb.velocity.magnitude < 0.2f)
		{
			Vector3 torso = aimCam.transform.position + 20 * aimCam.transform.forward;
			Ray ray = new Ray(aimCam.transform.position, aimCam.transform.forward);
			RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, aimMask);

			if (hits.Length <= 0)
			{
				weaponAimPoint.position = torso;
				torsoAimPoint.position = torso;
			}
			else
			{
				RaycastHit? bodyHit = null;
				List<RaycastHit> enviroHits = new List<RaycastHit>();

				foreach (var hit in hits)
				{
					bool isOwnCollider = false;

					if (hit.collider.gameObject.layer == 9)
					{
						enviroHits.Add(hit);
						continue;
					}

					// Check if the hit collider belongs to the player
					foreach (var collider in bodyColliders)
					{
						if (hit.collider == collider)
						{
							isOwnCollider = true;
							break;
						}
					}

					// if (bodyHit.HasValue)
					// {
					// 	Vector3 targetPoint = bodyHit.Value.point;
					// 	// Rotate the arm/gun to aim at the targetPoint
					// 	weaponAimPoint.position = targetPoint;
					// 	return;
					// }

					// If the hit collider does not belong to the player, set it as the aim target
					if (!isOwnCollider && hit.collider.gameObject.layer == 6)
					{
						//Debug.Log(hit.collider.gameObject.layer);
						bodyHit = hit;
						break; // Exit the loop after finding the first valid aim target
					}
				}

				if (bodyHit.HasValue)
				{
					if (enviroHits.Count > 0)
					{
						enviroHits.Sort((hit1, hit2) => hit1.distance.CompareTo(hit2.distance));

						if (Vector3.Distance(rb.transform.position, bodyHit.Value.point) < Vector3.Distance(rb.transform.position, enviroHits[0].point))
						{
							Vector3 targetPoint = bodyHit.Value.point;
							weaponAimPoint.position = targetPoint;
						}
						else
						{
							Vector3 targetPoint = enviroHits[0].point;
							// Rotate the arm/gun to aim at the targetPoint
							weaponAimPoint.position = targetPoint;
						}
					}
					// Vector3 targetPoint = bodyHit.Value.point;
					// // Rotate the arm/gun to aim at the targetPoint
					// weaponAimPoint.position = targetPoint;
				}
				else if (enviroHits.Count > 0)
				{
					enviroHits.Sort((hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
					// Use the closest hit's point as the target point
					Vector3 targetPoint = enviroHits[0].point;
					// Rotate the arm/gun to aim at the targetPoint
					weaponAimPoint.position = targetPoint;
				}
				else
				{
					//weaponAimPoint.position = Vector3.Lerp(weaponAimPoint.position, torso, 0.2f);
					weaponAimPoint.position = torso;
				}
			}
			torsoAimPoint.position = torso;
		}
		else
		{
			Vector3 torso = aimCam.transform.position + 20 * aimCam.transform.forward;
			torsoAimPoint.position = torso;
			weaponAimPoint.position = torso;
		}


		// Vector3 torso = aimCam.transform.position + 20 * aimCam.transform.forward;
		// if (Physics.Raycast(aimCam.transform.position, aimCam.transform.forward, out hit, Mathf.Infinity, aimMask))
		// {
		// 	//Debug.DrawRay(headObject.transform.position, headObject.transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
		// 	//Debug.Log(hit.distance);
		// 	//weaponAimPoint.position = Vector3.Lerp(weaponAimPoint.position, hit.point, 0.2f);
		// 	weaponAimPoint.position = hit.point;
		// }
		// else
		// {
		// 	//weaponAimPoint.position = Vector3.Lerp(weaponAimPoint.position, torso, 0.2f);
		// 	weaponAimPoint.position = torso;
		// }
		// torsoAimPoint.position = torso;
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
		// if (cooling.isOverheated)
		// {
		// 	cooling.CooldownOverheated();
		// }
		// else
		// {
		if (rb.velocity.magnitude < 0.05f)
		{
			cooling.SetStandingStill(true);
		}
		else
		{
			cooling.SetStandingStill(false);
		}
		// 	else
		// 	{
		// 		cooling.Cooldown();
		// 	}
		// }
		// cooling.PassiveCooldown();

		//TODO Temporary heating gauge visual
		coolingGauge.transform.localScale = coolingGaugeScaleCache * Mathf.Clamp((heatContainer.currentTemperature + 0.01f) / cooling.GetMaxHeat(), 0, 1f);
	}

	private void doSiphoning()
	{
		if (input.getSiphon())
		{
			siphon.ToggleSiphon();
		}
		else
		{
			siphon.NotSiphoning();
		}
	}

	private void setJointStrength()
	{
		float overheated = cooling.isOverheated ? 0.2f : 1f;

		tempJoint = upperTorsoJoint.slerpDrive;
		tempJoint.positionSpring = (legs.taggingModifier / 100f * 100000 * overheated) + 1000;
		upperTorsoJoint.slerpDrive = tempJoint;

		tempJoint = middleTorsoJoint.slerpDrive;
		tempJoint.positionSpring = (legs.taggingModifier / 100f * 1000000 * overheated) + 1000;
		middleTorsoJoint.slerpDrive = tempJoint;

		tempJoint = upperRightArmJoint.slerpDrive;
		tempJoint.positionSpring = (weapons.disruptionModifier / 100f * 800000 * overheated) + 1000;
		upperRightArmJoint.slerpDrive = tempJoint;

		//TODO Temporary tagging gauge visual
		taggingGauge.transform.localScale = taggingGaugeScaleCache * Mathf.Clamp((legs.taggingModifier + 0.01f) / 100f, 0, 1f);
	}

	private void SetRigPosture()
	{
		float posture = legs.taggingModifier / 100;

		upperTorsoMac.data.sourceObjects.SetWeight(0, posture);
		upperTorsoMac.data.sourceObjects.SetWeight(1, 1 - posture);

		lowerTorsoMac.data.sourceObjects.SetWeight(0, posture);
		lowerTorsoMac.data.sourceObjects.SetWeight(1, 1 - posture);

		var a = upperTorsoMac.data.sourceObjects;
		var a0 = a[0];
		var a1 = a[1];
		a0.weight = posture;
		a1.weight = 1 - posture;
		a[0] = a0;
		a[1] = a1;
		upperTorsoMac.data.sourceObjects = a;

		a = lowerTorsoMac.data.sourceObjects;
		a0 = a[0];
		a1 = a[1];
		a0.weight = posture;
		a1.weight = 1 - posture;
		a[0] = a0;
		a[1] = a1;
		lowerTorsoMac.data.sourceObjects = a;

		taggingTarget.rotation = Quaternion.Euler(270 + (30 * (1 - posture)), 0, 180);
	}

	//public void StartCooling()
	//{
	//    if (decrementCoroutine == null)
	//    {
	//        decrementCoroutine = StartCoroutine(cooling.DecreaseHeatCoroutine());
	//    }
	//}

	// public void StopCooling()
	// {
	// 	cooling.ResetCooldown();
	// }

	// Update is called once per frame
	void Update()
	{
		if (!isDead)
		{
			DoRotation();
			GetAimPoint();
		}
	}

	private void FixedUpdate()
	{
		if (!isDead)
		{
			ExecutePhysicsBasedInputs();
			doSiphoning();
		}
		legs.DoMoveDeacceleration();
		legs.RecoverFromTagging();
		legs.UpdateMovementTick(Time.deltaTime);
		weapons.RecoverFromDisruption();
		doCooling();
		setJointStrength();
		SetRigPosture();

		// if (isAI)
		// {
		// 	Debug.Log(legs.taggingModifier);
		// }
	}
}
