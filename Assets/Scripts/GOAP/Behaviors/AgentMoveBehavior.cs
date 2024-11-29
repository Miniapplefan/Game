using CrashKonijn.Goap.Behaviours;
using CrashKonijn.Goap.Interfaces;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentMoveBehavior : MonoBehaviour
{
	private BodyController bodyController;
	private AIController AIController;
	private NavMeshAgent NavMeshAgent;
	private AgentBehaviour AgentBehaviour;
	private ITarget CurrentTarget;
	[SerializeField] private float MinMoveDistance = 0.25f;
	private Vector3 EyeLevel = new Vector3(0, 2.33f, 0);
	private Vector3 LastPosition;

	private void Awake()
	{
		NavMeshAgent = GetComponent<NavMeshAgent>();
		AgentBehaviour = GetComponent<AgentBehaviour>();
		AIController = GetComponentInChildren<AIController>();
		bodyController = GetComponentInChildren<BodyController>();
	}

	private void OnEnable()
	{
		//AgentBehaviour.Events.OnTargetInRange += EventsOnTargetInRange;
		AgentBehaviour.Events.OnTargetChanged += EventsOnTargetChanged;
		AgentBehaviour.Events.OnTargetOutOfRange += EventsOnTargetOutOfRange;
	}

	private void OnDisable()
	{
		// AgentBehaviour.Events.OnTargetInRange -= EventsOnTargetInRange;
		AgentBehaviour.Events.OnTargetChanged -= EventsOnTargetChanged;
		AgentBehaviour.Events.OnTargetOutOfRange -= EventsOnTargetOutOfRange;
	}

	private void EventsOnTargetOutOfRange(ITarget target) { }

	private void EventsOnTargetChanged(ITarget target, bool inRange)
	{
		CurrentTarget = target;
		LastPosition = CurrentTarget.Position;
		NavMeshAgent.SetDestination(target.Position);
		NavMeshAgent.updatePosition = true;
		//AIController.SetAimTarget(target.Position + EyeLevel);
	}

	// private void EventsOnTargetInRange(ITarget target)
	// {
	//   CurrentTarget = target;
	// }

	private void Update()
	{
		//NavMeshAgent.acceleration = bodyController.legs.getMoveSpeed() * (bodyController.legs.moveAcceleration / 5) * Time.deltaTime;
		//NavMeshAgent.speed = 3.5f * bodyController.legs.getMoveSpeed();

		NavMeshAgent.speed = 3.5f * bodyController.legs.getMoveSpeed();

		// Vector3 vel = NavMeshAgent.velocity * (bodyController.legs.getMoveSpeed() / 5);

		// NavMeshAgent.velocity.Set(vel.x, vel.y, vel.z);
		// Debug.Log(NavMeshAgent.velocity);

		if (CurrentTarget == null)
		{
			return;
		}

		if (MinMoveDistance <= Vector3.Distance(CurrentTarget.Position, LastPosition))
		{
			LastPosition = CurrentTarget.Position;
			NavMeshAgent.SetDestination(CurrentTarget.Position);
			//AIController.SetAimTarget(CurrentTarget.Position + EyeLevel);
		}
	}

}