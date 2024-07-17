using CrashKonijn.Goap.Behaviours;
using CrashKonijn.Goap.Classes;
using CrashKonijn.Goap.Enums;
using CrashKonijn.Goap.Interfaces;
using UnityEngine;

public class OverheatHostileAction : ActionBase<AttackData>, IInjectable
{
	private AttackConfigSO AttackConfig;
	private Collider[] Colliders = new Collider[1];

	public override void Start(IMonoAgent agent, AttackData data)
	{
		data.Timer = AttackConfig.AttackDelay;
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
			if (data.targetState != null && data.targetState.bodyIsOverheated)
			{
				data.AIController.SetAimTarget(data.targetState.head.bounds.center);
			}
			else
			{
				data.AIController.SetAimTarget(Colliders[0].transform.position);
				//data.AIController.SetAimTarget(data.targetState.head.transform.position);
			}
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

	private void chooseWeaponToUse()
	{
		int weapon = Random.Range(0, 2);
		switch (weapon)
		{
			case 1:
				break;
		}

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