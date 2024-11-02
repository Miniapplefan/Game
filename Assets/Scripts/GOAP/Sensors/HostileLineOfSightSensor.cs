using CrashKonijn.Goap.Classes;
using CrashKonijn.Goap.Interfaces;
using CrashKonijn.Goap.Sensors;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.AI;

public class HostileLineOfSightSensor : LocalWorldSensorBase, IInjectable
{

	private Collider[] Colliders = new Collider[1];
	private AttackConfigSO AttackConfig;

	public override void Created()
	{
	}

	public override void Update()
	{
	}

	public override SenseValue Sense(IMonoAgent agent, IComponentReference references)
	{
		bool enemyHasLOS = false;
		//		Debug.Log(AttackConfig == null ? "AttackConfig Is null" : "AttackConfig Is OK");
		if (Physics.OverlapSphereNonAlloc(agent.transform.position, AttackConfig.SensorRadius, Colliders, AttackConfig.AttackableLayerMask) > 0)
		{
			//Player is in range, check if we can see them
			RaycastHit hit1;
			Vector3 direction1 = (Colliders[0].transform.position - agent.transform.position).normalized;
			if (Physics.Raycast(agent.transform.position, direction1, out hit1, Mathf.Infinity, AttackConfig.AttackableLayerMask | AttackConfig.ObstructionLayerMask))
			{
				enemyHasLOS = true;
			}
			else
			{
				enemyHasLOS = false;
			}
		}
		return new SenseValue(enemyHasLOS == true ? 1 : 0);
		//return new SenseValue(Mathf.CeilToInt(references.GetCachedComponent<NPCBrain>().bodyState.HeatContainer_getCurrentHeat()));
	}

	public void Inject(DependencyInjector injector)
	{
		AttackConfig = injector.AttackConfig;
	}
}