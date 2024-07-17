using CrashKonijn.Goap.Behaviours;
using CrashKonijn.Goap.Classes;
using CrashKonijn.Goap.Enums;
using CrashKonijn.Goap.Interfaces;
using UnityEngine;

public class CooldownAction : ActionBase<CommonData>, IInjectable
{
	private AttackConfigSO AttackConfig;
	private Collider[] Colliders = new Collider[1];


	public override void Created() { }

	public override void Start(IMonoAgent agent, CommonData data)
	{
		data.Timer = Random.Range(1, 2);
	}

	public override ActionRunState Perform(IMonoAgent agent, CommonData data, ActionContext context)
	{
		data.Timer -= context.DeltaTime;

		// if (data.Timer > 0)
		// {
		//   return ActionRunState.Continue;
		// }

		if (data.Timer > 0)
		{
			return ActionRunState.Continue;
		}

		bool seePlayer = false;
		if (Physics.OverlapSphereNonAlloc(agent.transform.position, AttackConfig.SensorRadius, Colliders, AttackConfig.AttackableLayerMask) > 0)
		{
			Vector3 direction1 = (Colliders[0].transform.position - agent.transform.position).normalized;
			RaycastHit hit1;
			if (Physics.Raycast(agent.transform.position, direction1, out hit1, Mathf.Infinity, AttackConfig.AttackableLayerMask | AttackConfig.ObstructionLayerMask))
			{
				seePlayer = hit1.transform.GetComponent<PlayerController>() != null;
			}
		}

		if (seePlayer)
		{
			return ActionRunState.Stop;
		}

		if (data.bodyState.Cooling_getCurrentHeat() < 1)
		{
			return ActionRunState.Stop;
		}

		return ActionRunState.Continue;
	}

	public override void End(IMonoAgent agent, CommonData data) { }
	public void Inject(DependencyInjector injector)
	{
		AttackConfig = injector.AttackConfig;
	}
}