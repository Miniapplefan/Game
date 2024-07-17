using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BodyInfo;
using static Limb;

public class BodyController : MonoBehaviour
{
	public BodyInfo so_initialBodyStats;

	public BodyState bodyState;

	public InputController input;

	CoolingModel cooling;
	private Coroutine decrementCoroutine = null;
	public GameObject coolingGauge;
	Vector3 coolingGaugeScaleCache;

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
		SubscribeSystemEvents();
		bodyState.Init(systemControllers);
		rb = GetComponent<Rigidbody>();
		bodyColliders = GetComponentsInChildren<Collider>();

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
		}

		coolingGaugeScaleCache = coolingGauge.transform.localScale;
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
		weapons.RaiseFiredWeapon += cooling.IncreaseHeat;

		cooling.RaiseIncreasedHeat += StopCooling;
		cooling.RaiseOverheated += weapons.OnCoolingSystemOverheat;
		cooling.RaiseCooledDownFromOverheat += weapons.OnCoolingSystemCooledOff;

		cooling.RaiseOverheated += legs.OnCoolingSystemOverheat;
		cooling.RaiseCooledDownFromOverheat += legs.OnCoolingSystemCooledOff;


		head.RaiseDeath += Die;
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
		if (Time.time - lastRaycastTime >= raycastInterval)
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
					Vector3 targetPoint = bodyHit.Value.point;
					// Rotate the arm/gun to aim at the targetPoint
					weaponAimPoint.position = targetPoint;
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
		if (cooling.isOverheated)
		{
			cooling.CooldownOverheated();
		}
		else
		{
			if (rb.velocity.magnitude > 0.05f)
			{
				StopCooling();
			}
			else
			{
				cooling.Cooldown();
			}
		}
		cooling.PassiveCooldown();

		//TODO Temporary heating gauge visual
		coolingGauge.transform.localScale = coolingGaugeScaleCache * Mathf.Clamp((cooling.currentHeat + 0.01f) / cooling.getMaxHeat(), 0, 1f);
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
		doSiphoning();
	}
}
