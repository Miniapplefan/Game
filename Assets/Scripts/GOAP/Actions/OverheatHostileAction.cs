using System;
using System.Linq;
using System.Collections.Generic;
using CrashKonijn.Goap.Behaviours;
using CrashKonijn.Goap.Classes;
using CrashKonijn.Goap.Enums;
using CrashKonijn.Goap.Interfaces;
using UnityEngine;

public class OverheatHostileAction : ActionBase<AttackData>, IInjectable
{
	private AttackConfigSO AttackConfig;
	private Collider[] Colliders = new Collider[1];

	public struct LimbConsideration
	{
		public LimbToTarget limb;
		public Func<float> considerationFunction;

		public LimbConsideration(LimbToTarget l, Func<float> considerationFunction)
		{
			this.limb = l;
			this.considerationFunction = considerationFunction;
		}

		public float Consideration()
		{
			return considerationFunction();
		}
	}

	public List<LimbConsideration> limbConsiderations;

	public enum LimbToTarget { Head, RightArm, RightLeg, LeftLeg };

	public override void Start(IMonoAgent agent, AttackData data)
	{
		data.Timer = AttackConfig.AttackDelay;
		limbConsiderations = new List<LimbConsideration>();
		limbConsiderations.Add(new LimbConsideration(LimbToTarget.Head, () => ConsiderHead(agent, data)));
		limbConsiderations.Add(new LimbConsideration(LimbToTarget.RightArm, () => ConsiderRightArm(agent, data)));
		limbConsiderations.Add(new LimbConsideration(LimbToTarget.RightLeg, () => ConsiderLegs(agent, data)));
		limbConsiderations.Add(new LimbConsideration(LimbToTarget.LeftLeg, () => ConsiderLegs(agent, data)));

	}
	public override void Created()
	{
	}

	public override ActionRunState Perform(IMonoAgent agent, AttackData data, ActionContext context)
	{
		data.Timer -= context.DeltaTime;
		if (Physics.OverlapSphereNonAlloc(agent.transform.position, AttackConfig.SensorRadius, Colliders, AttackConfig.AttackableLayerMask) > 0)
		{
			// if (data.targetState != null && data.targetState.bodyIsOverheated)
			// {
			// 	data.AIController.SetAimTarget(data.targetState.head.transform.position);
			// }

			if (data.targetState != null)
			{
				List<LimbConsideration> targetableLimbs = sortLimbsToTarget(agent, data);
				foreach (var limbConsideration in targetableLimbs)
				{
					Vector3 limbPos = LimbToPosition(limbConsideration.limb, data);
					if (!IsLimbObstructed(limbPos, agent))
					{
						data.AIController.SetAimTarget(limbPos);
						break;
					}
				}
			}
			else
			{
				data.AIController.SetAimTarget(Colliders[0].transform.position);
			}

			// if (data.targetState != null && data.targetState.bodyIsOverheated)
			// {
			// 	data.AIController.SetAimTarget(data.targetState.head.bounds.center);
			// }
			// else
			// {
			// 	data.AIController.SetAimTarget(Colliders[0].transform.position);
			// 	//data.AIController.SetAimTarget(data.targetState.head.transform.position);
			// }
		}

		Vector3 direction1 = (Colliders[0].transform.position - agent.transform.position).normalized;
		RaycastHit hit1;
		bool seePlayer = false;
		if (Physics.Raycast(agent.transform.position, direction1, out hit1, Mathf.Infinity, AttackConfig.AttackableLayerMask | AttackConfig.ObstructionLayerMask))
		{
			if (hit1.transform.GetComponent<PlayerController>() != null)
			{
				seePlayer = true;
				data.targetState = hit1.transform.GetComponent<BodyState>();
				data.bodyState.targetBodyState = data.targetState;
			}
		}

		bool shouldAttack = seePlayer
		&& data.bodyState.weapons.weaponRb.angularVelocity.magnitude < 0.5f
		&& data.navMeshAgent.velocity.magnitude < 0.05f;
		if (shouldAttack)
		{
			// if (data.targetState.bodyIsOverheated)
			// {
			// 	data.AIController.SetAimTarget(data.targetState.head.transform.position);
			// }

			//Debug.Log("Firing Weapon");
			// Debug.Log(data.bodyState.Weapons_weapon1Powered());
			// while (!data.bodyState.Weapons_weapon1Powered())
			// {
			//   data.AIController.didScroll = true;
			// }

			if (data.bodyState.Weapons_currentlyFiringBurst())
			{
				return ActionRunState.Continue;
			}

			if (data.bodyState.Weapons_weapon1Charged() && data.bodyState.Weapons_weapon1Powered())
			{
				data.AIController.pressingFire1 = true;
				return ActionRunState.Continue;
			}
			else if (data.bodyState.Weapons_weapon2Charged() && data.bodyState.Weapons_weapon2Powered())
			{
				data.AIController.pressingFire2 = true;
				return ActionRunState.Continue;
			}
			else if (data.bodyState.Weapons_weapon3Charged() && data.bodyState.Weapons_weapon3Powered())
			{
				data.AIController.pressingFire3 = true;
				return ActionRunState.Continue;
			}
			else
			{
				//data.bodyState.weapons.CycleToNextPowerAllocationDictionary();
				return ActionRunState.Stop;
			}
		}
		return data.Timer > 0 ? ActionRunState.Continue : ActionRunState.Stop;
	}

	private Vector3 LimbToPosition(LimbToTarget limb, AttackData data)
	{
		switch (limb)
		{
			case LimbToTarget.Head:
				return data.targetState.head.bounds.center;
			case LimbToTarget.RightArm:
				return Colliders[0].transform.position;
			case LimbToTarget.RightLeg:
				return data.targetState.rightLeg.bounds.center;
			case LimbToTarget.LeftLeg:
				return data.targetState.leftLeg.bounds.center;
			default:
				return data.targetState.rightArm.bounds.center;
		}
	}

	private bool IsLimbObstructed(Vector3 limbPos, IMonoAgent agent)
	{
		Vector3 directionToLimb = (limbPos - agent.transform.position).normalized;
		float distanceToLimb = Vector3.Distance(agent.transform.position, limbPos);
		RaycastHit hit;
		if (Physics.Raycast(agent.transform.position, directionToLimb, out hit, distanceToLimb, AttackConfig.ObstructionLayerMask))
		{
			// Limb is obstructed if something was hit
			return true;
		}
		return false;
	}

	private List<LimbConsideration> sortLimbsToTarget(IMonoAgent agent, AttackData data)
	{
		// Sort the limb considerations by their utility in descending order
		List<LimbConsideration> sortedLimbConsiderations = limbConsiderations
				.OrderByDescending(lc => lc.Consideration())
				.ToList();

		return sortedLimbConsiderations;
	}

	public float ConsiderHead(IMonoAgent agent, AttackData data)
	{
		float hostileOverheated = data.targetState.bodyIsOverheated ? 1 : 0;

		return hostileOverheated;
	}

	public float ConsiderRightArm(IMonoAgent agent, AttackData data)
	{

		return 0.5f;
	}

	public float ConsiderLegs(IMonoAgent agent, AttackData data)
	{
		float hostileTagging = data.targetState.Legs_getTaggingHealth() / 100;

		return hostileTagging;
	}

	private void chooseWeaponToUse()
	{
		// int weapon = Random.Range(0, 2);
		// switch (weapon)
		// {
		// 	case 1:
		// 		break;
		// }

	}

	public override void End(IMonoAgent agent, AttackData data)
	{
		data.AIController.pressingFire1 = false;
		data.AIController.pressingFire2 = false;
		data.AIController.pressingFire3 = false;
	}

	public void Inject(DependencyInjector injector)
	{
		AttackConfig = injector.AttackConfig;
	}
}