using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
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

	[HideInInspector]
	public CoolingModel cooling;

	[HideInInspector]
	public HeatContainer heatContainer;
	private Coroutine decrementCoroutine = null;
	public GameObject coolingGauge;
	Vector3 coolingGaugeScaleCache;

	public GameObject taggingGauge;
	Vector3 taggingGaugeScaleCache;
	public TMP_Text dollarsIndicator;
	public TMP_Text healthIndicator;

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

	public Transform siphonArm;

	List<SystemModel> systemControllers;
	public Rigidbody rb;
	public Rigidbody ragdollCore;
	public GameObject headObject;
	public GameObject aimCam;
	public bool isKnockbacked = false;
	private float knockbackTimer;

	private NavMeshAgent agent;
	private Vector3 agentDestination;
	private float minKnockbackDuration = 0.000001f;

	// used to be 50f
	public float repairDelay = 10f;
	public Dictionary<RepairTarget, float> damagedLimbs = new Dictionary<RepairTarget, float>();
	private List<RepairTarget> toRepair = new List<RepairTarget>();

	public class RepairTarget
	{
		public SystemModel system { get; private set; }
		public LimbID specificLimb { get; private set; }

		public RepairTarget(SystemModel s, LimbID l = LimbID.none)
		{
			system = s;
			specificLimb = l;
		}

		public override bool Equals(object obj)
		{
			if (obj is RepairTarget other)
			{
				return system == other.system && specificLimb == other.specificLimb;
			}
			return false;
		}

		public override int GetHashCode()
		{
			int hash = system.GetHashCode();
			if (specificLimb != LimbID.none)
				hash = hash * 31 + specificLimb.GetHashCode();
			return hash;
		}
	}

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
			agent = GetComponentInParent<NavMeshAgent>();
			isAI = true;
		}

		coolingGaugeScaleCache = coolingGauge.transform.localScale;
		taggingGaugeScaleCache = taggingGauge.transform.localScale;
		healthIndicator.text = head.health.ToString();
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
					siphon = new SiphonModel(so_initialBodyStats.rawSystemStartLevels[i], siphonHead, siphonArm);
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
		ApplyKnockback(i.impactVector, i.limb);
		if (cooling.isOverheated)
		{
			DamageSystem(i);
		}
		else
		{
			//heatContainer.IncreaseHeat(this, i.amount);
			//cooling.IncreaseHeat(this, i.amount);
		}
	}

	public void DamageSystem(DamageInfo i)
	{
		head.Damage(1);
		if (i.limb.specificLimb == Limb.LimbID.none)
		{
			GetSystem(i.limb.linkedSystem).Damage(1);
			checkForRepair(i);
		}
		else
		{
			switch (i.limb.specificLimb)
			{
				case LimbID.leftLeg:
					legs.damageLeftLeg(1);
					checkForRepair(i);
					break;
				case LimbID.rightLeg:
					legs.damageRightLeg(1);
					checkForRepair(i);
					break;
				case LimbID.head:
					//head.Damage((int)i.amount);
					break;
			}
		}
	}

	void checkForRepair(DamageInfo i)
	{
		RepairTarget target;
		target = new RepairTarget(GetSystem(i.limb.linkedSystem), i.limb.specificLimb);
		if (!damagedLimbs.ContainsKey(target))
		{
			damagedLimbs.Add(target, Time.time + repairDelay);
		}
		else
		{
			// Reset timer if already damaged
			damagedLimbs[target] = Time.time + repairDelay;
		}
	}

	public void doLimbRepairs()
	{
		// if (legs.leftLegHealth < legs.currentLevelWithoutDamage)
		// {
		// 	RepairTarget target;
		// 	target = new RepairTarget(legs, LimbID.leftLeg);
		// 	if (!damagedLimbs.ContainsKey(target))
		// 	{
		// 		damagedLimbs.Add(target, Time.time + repairDelay);
		// 		Debug.Log("add lleg repair");
		// 	}
		// 	else
		// 	{
		// 		Debug.Log(damagedLimbs[target] + " || " + Time.time);
		// 	}
		// }

		// if (legs.rightLegHealth < legs.currentLevelWithoutDamage)
		// {
		// 	RepairTarget target;
		// 	target = new RepairTarget(legs, LimbID.rightLeg);
		// 	if (!damagedLimbs.ContainsKey(target))
		// 	{
		// 		damagedLimbs.Add(target, Time.time + repairDelay);
		// 		Debug.Log("add rleg repair");
		// 	}
		// 	else
		// 	{
		// 		Debug.Log(damagedLimbs[target] + " || " + Time.time);
		// 	}
		// }

		foreach (var entry in damagedLimbs)
		{
			// Debug.Log(entry.Key.specificLimb + " : " + entry.Value + "-||-" + Time.time);
			SystemModel limb = entry.Key.system;
			float repairTime = entry.Value;

			if (Time.time >= repairTime)
			{
				head.Repair(1);
				if (entry.Key.specificLimb == Limb.LimbID.none)
				{
					limb.Repair(1);
				}
				else
				{
					switch (entry.Key.specificLimb)
					{
						case LimbID.leftLeg:
							legs.healLeftLeg(1);
							if ((entry.Key.specificLimb == LimbID.leftLeg && legs.leftLegHealth == limb.currentLevelWithoutDamage))
							{
								toRepair.Add(entry.Key);
								// Debug.Log("lleg done");
							}
							break;
						case LimbID.rightLeg:
							legs.healRightLeg(1);
							if ((entry.Key.specificLimb == LimbID.rightLeg && legs.rightLegHealth == limb.currentLevelWithoutDamage))
							{
								toRepair.Add(entry.Key);
								// Debug.Log("rleg done");
							}
							break;
						case LimbID.head:
							// head.Repair(1);
							break;
					}
				}
				if (limb.currentLevelWithoutDamage == limb.currentLevel && entry.Key.system != legs)
				{
					Debug.Log("Repaired " + limb.name);
					toRepair.Add(entry.Key);

					// string dict = "[";
					// foreach (RepairTarget l in toRepair)
					// {
					// 	dict += l.system + ", " + l.specificLimb + " | ";
					// }
					// dict += "]";
					// Debug.Log(dict);
				}
			}
			else
			{
				//Debug.Log("Time current: " + Time.time + " Time of repair: " + repairTime);
			}
		}
		// var dlen = toRepair.ToArray().Length;
		// Remove fully repaired limbs
		foreach (RepairTarget limb in toRepair)
		{
			// Debug.Log(limb.specificLimb + " was fully repaired");
			damagedLimbs.Remove(limb);
		}
		toRepair.Clear();

		// foreach (var entry in damagedLimbs)
		// {
		// 	Debug.Log(entry.Key.specificLimb + " : " + entry.Value + "-||-" + Time.time);
		// }

		// var dlenafter = toRepair.ToArray().Length;

		// if (dlen > dlenafter)
		// {
		// 	string dict = "[";
		// 	foreach (RepairTarget limb in toRepair)
		// 	{
		// 		dict += limb.system + ", " + limb.specificLimb + " | ";
		// 	}
		// 	dict += "]";
		// 	Debug.Log(dict);
		// }
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

	private void setWeaponGauges()
	{
		weapon1gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[0] && weapons.guns[0].isCharged());
		weapon2gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[1] && weapons.guns[1].isCharged());
		weapon3gauge.SetActive(weapons.GetCurrentPowerAllocationDictionary()[2] && weapons.guns[2].isCharged());
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

	private Vector3 KnockbackHeightCheck = new Vector3(0, 1f, 0);
	private void ApplyKnockback(Vector3 force, Limb l)
	{
		isKnockbacked = true;
		if (isAI)
		{
			agentDestination = agent.destination;
		}
		// if (agent != null)
		// {
		// 	agent.enabled = false;
		// }

		bool backTooCloseToWall = false;
		RaycastHit hit;
		if (Physics.Raycast(transform.position + KnockbackHeightCheck, -transform.forward, out hit, 2.0f, aimMask))
		{
			backTooCloseToWall = true;
		}

		if (!backTooCloseToWall)
		{
			if (cooling.isOverheated)
			{
				rb.AddForce((force * 2));

			}
			else
			{
				rb.AddForce((force / 3) * (1 - legs.getTagging()) * GetKnockbackFromLimb(l));
			}
		}
		else
		{
			rb.AddForce((force / 8) * (1 - legs.getTagging()) * GetKnockbackFromLimb(l));
		}
		knockbackTimer = minKnockbackDuration;
	}

	public float GetKnockbackFromLimb(Limb l)
	{
		switch (l.specificLimb)
		{
			case Limb.LimbID.leftLeg:
				return 0.5f;
			case Limb.LimbID.rightLeg:
				return 0.5f;
			case Limb.LimbID.torso:
				return 1f;
			case Limb.LimbID.head:
				return 0.75f;
			default:
				return 0.2f;
		}
	}

	private void HandleKnockback()
	{
		if (knockbackTimer > 0 && isKnockbacked == true)
		{
			knockbackTimer -= Time.deltaTime;
		}
		else
		{
			if (rb.velocity.magnitude < 0.05f && rb.velocity.y < 0.01f && isKnockbacked == true)
			{
				NavMeshAgent agent = GetComponentInParent<NavMeshAgent>();
				isKnockbacked = false;
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				transform.localPosition.Set(0, transform.localPosition.y, 0);
				// Transform parentTransform = GetComponentInParent<Transform>();
				// parentTransform.position = transform.position;
				if (agent != null)
				{
					float yPos = transform.localPosition.y;
					agent.enabled = true;
					agent.Warp(rb.transform.position);
					rb.transform.localPosition = new Vector3(0, yPos, 0);
					// Debug.Log(agent.hasPath);
					agent.ResetPath();
					agent.SetDestination(agentDestination);
					//this.transform.SetParent(parent.transform, true);
				}
			}
		}
		// if (agent != null && Vector3.Distance(agent.transform.position, rb.transform.position) > 0.01f)
		// {
		// 	agent.Warp(rb.transform.position);
		// }
	}

	private void ClampRigidbodyYPos()
	{
		float yPos = Mathf.Clamp(rb.transform.position.y, 1.6f, Mathf.Infinity);
		rb.transform.position = new Vector3(rb.transform.position.x, yPos, rb.transform.position.z);
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
		float overheated = cooling.isOverheated ? 0.1f : 1f;

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
		float posture = bodyState.Cooling_IsOverheated() ? 0.001f : legs.taggingModifier / 100;

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
			doLimbRepairs();
		}
		legs.DoMoveDeacceleration();
		legs.RecoverFromTagging(1 - Mathf.Pow(heatContainer.currentTemperature / cooling.GetMaxHeat(), 2));
		legs.UpdateMovementTick(Time.deltaTime);
		weapons.RecoverFromDisruption();
		doCooling();
		HandleKnockback();
		ClampRigidbodyYPos();
		setJointStrength();
		SetRigPosture();

		setWeaponGauges();
		dollarsIndicator.text = (Mathf.Round(siphon.dollars * 100f) / 100f).ToString();
		healthIndicator.text = head.health.ToString();
		// if (isAI)
		// {
		// 	Debug.Log(agent.hasPath);
		// }
	}
}
