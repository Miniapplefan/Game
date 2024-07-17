using System.Collections.Generic;
using CrashKonijn.Goap.Classes;
using CrashKonijn.Goap.Interfaces;
using CrashKonijn.Goap.Sensors;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class HostileTargetSensor : LocalTargetSensorBase, IInjectable
{

	private AttackConfigSO AttackConfig;

	public float circleRadius = 5f;
	public int numberOfPoints = 36;
	private Collider[] Colliders = new Collider[1];

	public override void Created()
	{
	}

	public override void Update()
	{
	}

	public override ITarget Sense(IMonoAgent agent, IComponentReference references)
	{
		if (Physics.OverlapSphereNonAlloc(agent.transform.position, AttackConfig.SensorRadius, Colliders, AttackConfig.AttackableLayerMask) > 0)
		{

			//Player is in range, check if we can see them
			RaycastHit hit1;
			Vector3 direction1 = (Colliders[0].transform.position - agent.transform.position).normalized;
			if (Physics.Raycast(agent.transform.position, direction1, out hit1, Mathf.Infinity, AttackConfig.AttackableLayerMask | AttackConfig.ObstructionLayerMask))
			{
				if (hit1.transform.GetComponent<PlayerController>() != null)
				{
					//Debug.Log("Can already see player, staying put");
					return new PositionTarget(agent.transform.position);
				}
			}

			//float distanceToPlayer = Vector3.Distance(agent.transform.position, Colliders[0].transform.position);

			//int count = 0;

			//We do not see the player from our position
			// while (count < 5)
			// {
			// 	Vector3 randomPointOnCircle = GetRandomPointOnCircle(Colliders[0].transform.position, distanceToPlayer);
			// 	float distance = Vector3.Distance(agent.transform.position, randomPointOnCircle);

			// 	if (distance > distanceToPlayer)
			// 	{
			// 		continue;
			// 	}
			// 	Vector3 direction2 = (Colliders[0].transform.position - randomPointOnCircle).normalized;
			// 	RaycastHit hit2;
			// 	if (Physics.Raycast(randomPointOnCircle, direction2, out hit2, Mathf.Infinity, AttackConfig.AttackableLayerMask | AttackConfig.ObstructionLayerMask))
			// 	{
			// 		if (hit2.transform.GetComponent<PlayerController>() != null)
			// 		{
			// 			//Debug.Log("Do not see player, moving");
			// 			return new PositionTarget(randomPointOnCircle);
			// 		}
			// 	}
			// 	count++;
			// }

			List<Vector3> points = new List<Vector3>();
			float angleStep = 360f / numberOfPoints;

			for (int i = 0; i < numberOfPoints; i++)
			{
				float angle = i * angleStep;
				Vector3 point = agent.transform.position + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * circleRadius;
				points.Add(point);
			}

			Vector3 closestPoint = Vector3.zero;
			float closestDistance = float.MaxValue;

			foreach (Vector3 point in points)
			{
				//Debug.Log("checking points");
				float distanceToPlayer = Vector3.Distance(agent.transform.position, Colliders[0].transform.position);
				float distanceToAI = Vector3.Distance(point, agent.transform.position);

				if (distanceToAI < distanceToPlayer && HasLineOfSight(point, Colliders[0].transform.position))
				{
					//Debug.Log("point within range and has los");
					if (distanceToAI < closestDistance)
					{
						//Debug.Log("closer point found");
						closestDistance = distanceToAI;
						closestPoint = point;
					}
				}
			}

			if (closestPoint != Vector3.zero)
			{
				return new PositionTarget(closestPoint);
			}
			Debug.Log("no point found");
			//return new PositionTarget(Colliders[0].transform.position);

		}

		// if (Physics.OverlapSphereNonAlloc(agent.transform.position, AttackConfig.SensorRadius, Colliders, AttackConfig.AttackableLayerMask) > 0)
		// {
		//   Transform target = Colliders[0].transform;
		//   Vector3 directionToTarget = (target.position - agent.transform.position).normalized;

		//   Debug.Log("overlapping");
		//   if (Vector3.Angle(agent.transform.forward, directionToTarget) < AttackConfig.FOVAngle / 2)
		//   {
		//     float distanceToTarget = Vector3.Distance(agent.transform.position, target.position);
		//     if (!Physics.Raycast(agent.transform.position, directionToTarget, distanceToTarget, AttackConfig.ObstructionLayerMask))
		//     {
		//       return new TransformTarget(target);
		//     }
		//     else
		//     {
		//       return null;
		//     }
		//   }
		//   else
		//   {
		//     return null;
		//   }
		// }
		return new PositionTarget(agent.transform.position);
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

	bool HasLineOfSight(Vector3 start, Vector3 end)
	{
		RaycastHit hit;
		if (Physics.Raycast(start, (end - start).normalized, out hit, Mathf.Infinity))
		{
			//Debug.Log(hit.transform.GetComponent<PlayerController>() != null);
			return hit.transform.GetComponent<PlayerController>() != null;
		}
		return false;
	}

	public void Inject(DependencyInjector injector)
	{
		AttackConfig = injector.AttackConfig;
	}
}