using CrashKonijn.Goap.Classes;
using CrashKonijn.Goap.Interfaces;
using CrashKonijn.Goap.Sensors;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.AI;
using CrashKonijn.Goap.Classes.References;

public class CoverTargetSensor : LocalTargetSensorBase, IInjectable
{
	private AttackConfigSO AttackConfig;
	private Collider[] TargetCollider = new Collider[1];
	private Collider[] Colliders = new Collider[10];
	private Vector3 currentPosition;
	private NavMeshAgent navMeshAgent;

	public override void Created()
	{
	}

	public override void Update()
	{
	}

	public override ITarget Sense(IMonoAgent agent, IComponentReference references)
	{
		currentPosition = agent.transform.position;
		navMeshAgent = agent.GetComponent<NavMeshAgent>();

		Vector3 position = GetCoverPosition(agent);

		return new PositionTarget(position);
	}

	private Vector3 GetCoverPosition(IMonoAgent agent)
	{
		int count = 0;

		if (Physics.OverlapSphereNonAlloc(agent.transform.position, AttackConfig.SensorRadius, Colliders, AttackConfig.AttackableLayerMask) > 0)
		{
			RaycastHit hit1;
			Vector3 direction1 = (Colliders[0].transform.position - agent.transform.position).normalized;
			if (Physics.Raycast(agent.transform.position, direction1, out hit1, Mathf.Infinity, AttackConfig.AttackableLayerMask | AttackConfig.ObstructionLayerMask))
			{
				if (hit1.transform.GetComponent<PlayerController>() != null)
				{
					float distanceToPlayer = Vector3.Distance(agent.transform.position, Colliders[0].transform.position);

					// TODO Huge frame drop spike related to this while loop. spread the work out over multiple frames somehow
					while (count < 10)
					{
						Vector3 randomPointOnCircle = GetRandomPointOnCircle(Colliders[0].transform.position, distanceToPlayer);
						float distance = Vector3.Distance(agent.transform.position, randomPointOnCircle);

						if (distance < distanceToPlayer)
						{
							Vector3 direction2 = (Colliders[0].transform.position - randomPointOnCircle).normalized;
							RaycastHit hit2;
							if (Physics.Raycast(randomPointOnCircle, direction2, out hit2, Mathf.Infinity, AttackConfig.AttackableLayerMask | AttackConfig.ObstructionLayerMask))
							{
								if (hit2.transform.GetComponent<PlayerController>() == null)
								{
									//Debug.Log("Do not see player, moving");
									return randomPointOnCircle;
								}
							}
						}
						count++;
					}
				}
			}
		}

		while (count < 50)
		{
			for (int i = 0; i < Colliders.Length; i++)
			{
				Colliders[i] = null;
			}

			int target = Physics.OverlapSphereNonAlloc(agent.transform.position, AttackConfig.SensorRadius, TargetCollider, AttackConfig.AttackableLayerMask);

			int hits = Physics.OverlapSphereNonAlloc(agent.transform.position, AttackConfig.SensorRadius, Colliders, AttackConfig.ObstructionLayerMask);

			int hitReduction = 0;
			for (int i = 0; i < hits; i++)
			{
				if (Vector3.Distance(Colliders[i].transform.position, TargetCollider[0].transform.position) < AttackConfig.MinPlayerDistance || Colliders[i].bounds.size.y < AttackConfig.MinObstacleHeight)
				{
					Colliders[i] = null;
					hitReduction++;
				}
			}
			hits -= hitReduction;

			System.Array.Sort(Colliders, ColliderArraySortComparer);

			for (int i = 0; i < hits; i++)
			{
				if (NavMesh.SamplePosition(Colliders[i].transform.position, out NavMeshHit hit, 4f, navMeshAgent.areaMask))
				{
					if (!NavMesh.FindClosestEdge(hit.position, out hit, navMeshAgent.areaMask))
					{
						Debug.LogError($"Unable to find edge close to {hit.position}");
					}

					if (Vector3.Dot(hit.normal, (TargetCollider[0].transform.position - hit.position).normalized) < AttackConfig.HideSensitivity)
					{
						return hit.position;
					}
					else
					{
						// Since the previous spot wasn't facing "away" enough from the target, we'll try on the other side of the object
						if (NavMesh.SamplePosition(Colliders[i].transform.position - (TargetCollider[0].transform.position - hit.position).normalized * 2, out NavMeshHit hit2, 2f, navMeshAgent.areaMask))
						{
							if (!NavMesh.FindClosestEdge(hit2.position, out hit2, navMeshAgent.areaMask))
							{
								Debug.LogError($"Unable to find edge close to {hit2.position} (second attempt)");
							}

							if (Vector3.Dot(hit2.normal, (TargetCollider[0].transform.position - hit2.position).normalized) < AttackConfig.HideSensitivity)
							{
								return hit2.position;
							}
						}
					}
				}
				else
				{
					Debug.LogError($"Unable to find NavMesh near object {Colliders[i].name} at {Colliders[i].transform.position}");
				}
			}
			count++;

		}
		return GetRandomPosition(agent);
	}

	public int ColliderArraySortComparer(Collider A, Collider B)
	{
		if (A == null && B != null)
		{
			return 1;
		}
		else if (A != null && B == null)
		{
			return -1;
		}
		else if (A == null && B == null)
		{
			return 0;
		}
		else
		{
			return Vector3.Distance(currentPosition, A.transform.position).CompareTo(Vector3.Distance(currentPosition, B.transform.position));
		}
	}

	private Vector3 GetRandomPosition(IMonoAgent agent)
	{
		int count = 0;

		while (count < 5)
		{
			Vector2 random = Random.insideUnitCircle * 10;
			Vector3 position = agent.transform.position + new UnityEngine.Vector3(
				random.x,
				0,
				random.y
			);

			if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1, NavMesh.AllAreas))
			{
				return hit.position;
			}

			count++;
		}

		return agent.transform.position;
	}

	private Vector3 GetRandomPointOnCircle(Vector3 center, float radius)
	{
		// Generate a random angle between 0 and 2π
		float randomAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);

		// Calculate the x and z coordinates of the random point on the circle
		float x = center.x + radius * Mathf.Cos(randomAngle);
		float z = center.z + radius * Mathf.Sin(randomAngle);

		// Set the y coordinate to the center's y coordinate (assuming the circle is on the same plane)
		float y = center.y;

		// Return the random point on the circle
		return new Vector3(x, y, z);
	}

	public void Inject(DependencyInjector injector)
	{
		AttackConfig = injector.AttackConfig;
	}
}