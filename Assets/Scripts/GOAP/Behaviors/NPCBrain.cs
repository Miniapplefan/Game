using CrashKonijn.Goap.Behaviours;
using UnityEngine;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(AgentBehaviour))]
public class NPCBrain : MonoBehaviour
{
	private AgentBehaviour AgentBehaviour;
	public BodyState bodyState;
	public float currentGoalInertia;
	public float maxInertia = 0.2f;
	public float lastDecisionTime = 0;
	public List<GoalConsideration> goals;

	public float ConsiderCooldownVal;
	public float ConsiderOverheatTargetVal;
	public float ConsiderDeploySiphonVal;
	public float ConsiderTakeCoverVal;


	private void Awake()
	{
		AgentBehaviour = GetComponent<AgentBehaviour>();
		bodyState = GetComponentInChildren<BodyState>();

		goals = new List<GoalConsideration>(); // Initialize the list
		goals.Add(new GoalConsideration(new CooldownGoal(), true, ConsiderCooldownGoal));
		goals.Add(new GoalConsideration(new OverheatHostileGoal(), false, ConsiderOverheatTargetGoal));
		goals.Add(new GoalConsideration(new DeploySiphonGoal(), true, ConsiderDeploySiphonGoal));
		goals.Add(new GoalConsideration(new TakeCoverGoal(), true, ConsiderTakeCoverGoal));
	}

	private void Start()
	{
		AgentBehaviour.SetGoal<OverheatHostileGoal>(false);
	}

	private void FixedUpdate()
	{
		ConsiderCooldownVal = ConsiderCooldownGoal();
		ConsiderOverheatTargetVal = ConsiderOverheatTargetGoal();
		ConsiderDeploySiphonVal = ConsiderDeploySiphonGoal();
		ConsiderTakeCoverVal = ConsiderTakeCoverGoal();

		if (bodyState.targetBodyState != null && bodyState.targetBodyState.Cooling_IsOverheated())
		{
			AgentBehaviour.SetGoal<OverheatHostileGoal>(false);
		}
		//else if (currentGoalInertia <= 0)
		// else
		// {
		// 	if (ConsiderDeploySiphonGoal() > ConsiderCooldownGoal())
		// 	{
		// 		AgentBehaviour.SetGoal<DeploySiphonGoal>(false);
		// 		//currentGoalInertia = (ConsiderDeploySiphonGoal() - ConsiderCooldownGoal()) * maxInertia;
		// 	}
		// 	else if (ConsiderCooldownGoal() > ConsiderOverheatTargetGoal())
		// 	{
		// 		AgentBehaviour.SetGoal<CooldownGoal>(false);
		// 		//currentGoalInertia = (ConsiderCooldownGoal() - ConsiderOverheatTargetGoal()) * maxInertia;
		// 	}
		// 	else
		// 	{
		// 		AgentBehaviour.SetGoal<OverheatHostileGoal>(false);
		// 		//currentGoalInertia = (ConsiderOverheatTargetGoal() - ConsiderCooldownGoal()) * maxInertia;
		// 	}
		// }
		// currentGoalInertia -= Time.deltaTime;
		else
		{
			if (currentGoalInertia <= 0)
			{
				GoalConsideration chosenGoal = GetHighestConsiderationGoal(goals);
				switch (chosenGoal.goal)
				{
					case CooldownGoal:
						AgentBehaviour.SetGoal<CooldownGoal>(chosenGoal.cancelable);
						currentGoalInertia = maxInertia;
						break;
					case OverheatHostileGoal:
						AgentBehaviour.SetGoal<OverheatHostileGoal>(chosenGoal.cancelable);
						currentGoalInertia = maxInertia;
						break;
					case DeploySiphonGoal:
						AgentBehaviour.SetGoal<DeploySiphonGoal>(chosenGoal.cancelable);
						currentGoalInertia = maxInertia;
						break;
					case TakeCoverGoal:
						AgentBehaviour.SetGoal<TakeCoverGoal>(chosenGoal.cancelable);
						currentGoalInertia = maxInertia;
						break;
					default:
						Debug.Log("AI Defaulting");
						AgentBehaviour.SetGoal<CooldownGoal>(chosenGoal.cancelable);
						break;
				}
			}
		}
		currentGoalInertia -= Time.deltaTime;
	}

	public struct GoalConsideration
	{
		public GoalBase goal;
		// decides if the ongoing action should terminate before setting the new goal.
		public bool cancelable;
		public Func<float> considerationFunction;

		public GoalConsideration(GoalBase goal, bool cancelable, Func<float> considerationFunction)
		{
			this.goal = goal;
			this.cancelable = cancelable;
			this.considerationFunction = considerationFunction;
		}

		public float Consideration()
		{
			return considerationFunction();
		}
	}

	private GoalConsideration GetHighestConsiderationGoal(List<GoalConsideration> goals)
	{
		if (goals == null || goals.Count == 0)
		{
			throw new ArgumentException("The list of goals cannot be null or empty");
		}

		return goals.Aggregate((maxGoal, currentGoal) =>
				currentGoal.Consideration() > maxGoal.Consideration() ? currentGoal : maxGoal);
	}

	private float ConsiderCooldownGoal()
	{
		float heatConsideration = Mathf.Pow((bodyState.HeatContainer_getCurrentHeat() / bodyState.cooling.GetMaxHeat()), 3);
		float weaponsChargedConsideration = bodyState.Weapons_numWeaponsCharged() == 0 ? 1 : 1 / bodyState.Weapons_numWeaponsCharged();

		return PositiveHeatConsideration()
		//* weaponsChargedConsideration
		;
	}

	private float ConsiderTakeCoverGoal()
	{
		return NegativeWeaponsChargedConsideration()
		* NegativeTaggingConsideration()
		* DeployedSiphonConsideration(1f, 0.3f);
	}

	private float ConsiderDeploySiphonGoal()
	{
		float heatConsideration = -(Mathf.Pow((bodyState.HeatContainer_getCurrentHeat() / bodyState.cooling.GetMaxHeat()), 3)) + 1;
		float target_heatConsideration = Mathf.Pow((bodyState.HeatContainer_getCurrentHeat() / bodyState.cooling.GetMaxHeat()), 2);
		float deployedConsideration = bodyState.Siphon_isExtended() ? 0 : 1;
		float weaponsChargedConsideration = bodyState.Weapons_numWeaponsCharged() == 0 ? 1 : 1 / bodyState.Weapons_numWeaponsCharged();

		return
		NegativeHeatConsideration()
		* Target_HeatConsideration()
		* DeployedSiphonConsideration(0f, 0.7f)
		 * SiphonAmountLeftConsideration();
		//* weaponsChargedConsideration
		;
	}

	private float ConsiderOverheatTargetGoal()
	{
		float heatConsideration = -(Mathf.Pow((bodyState.HeatContainer_getCurrentHeat() / bodyState.cooling.GetMaxHeat()), 3)) + 1;
		float weaponsChargedConsideration = bodyState.Weapons_numWeaponsCharged() == 0 ? 0 : bodyState.Weapons_numWeaponsCharged() / 3;
		// float target_heatConsideration = Mathf.Clamp(Mathf.Pow((bodyState.Cooling_getCurrentHeat() / bodyState.cooling.getMaxHeat()), 2), 0.8f, 1f);

		return NegativeHeatConsideration()
		* WeaponsChargedConsideration()
		* DeployedSiphonConsideration(0.5f, 1f)
		* AwareOfHostileConsideration();
		;
	}

	#region Considerations


	/// <summary>
	/// High score for low heat, low score for high heat, considering ambient temperature as the minimum possible heat
	/// </summary>
	/// <returns>float between 0 and 1</returns>
	private float NegativeHeatConsideration()
	{
		float currentHeat = bodyState.HeatContainer_getCurrentHeat();
		float maxHeat = bodyState.cooling.GetMaxHeat();
		float ambientTemperature = bodyState.heatContainer.GetAirTemperature();

		// Ensure we are not trying to cool below the ambient temperature
		float normalizedHeat = (currentHeat - ambientTemperature) / (maxHeat - ambientTemperature);

		return -(Mathf.Pow(normalizedHeat, 3)) + 1;
	}

	/// <summary>
	/// High score for high heat, low score for low heat, considering ambient temperature as the minimum possible heat
	/// </summary>
	/// <returns>float between 0 and 1</returns>
	private float PositiveHeatConsideration()
	{
		float currentHeat = bodyState.HeatContainer_getCurrentHeat();
		float maxHeat = bodyState.cooling.GetMaxHeat();
		float ambientTemperature = bodyState.heatContainer.GetAirTemperature();

		// Ensure we are not trying to cool below the ambient temperature
		float normalizedHeat = (currentHeat - ambientTemperature) / (maxHeat - ambientTemperature);
		//Debug.Log(bodyState.heatContainer.GetAirTemperature());
		return Mathf.Pow(normalizedHeat, 3);
	}

	/// <summary>
	/// High score for being tagged a lot, low score for not having much tagging
	/// </summary>
	/// <returns>float between 0 and 1</returns>
	private float NegativeTaggingConsideration()
	{
		return -(Mathf.Pow((bodyState.Legs_getTaggingHealth() / 100), 3)) + 1;
	}

	/// <summary>
	/// Higher score based on number of weapons charged, 0 if no weapons charged 
	/// </summary>
	/// <returns>float between 0 and 1</returns>
	private float WeaponsChargedConsideration()
	{
		return bodyState.Weapons_numWeaponsCharged() == 0 ? 0 : bodyState.Weapons_numWeaponsCharged() / 3;
	}

	/// <summary>
	/// Higher score based on less number of weapons charged, 0 if all weapons charged 
	/// </summary>
	/// <returns>float between 0 and 1</returns>
	private float NegativeWeaponsChargedConsideration()
	{
		// If all weapons are charged, return 0; if none are charged, return 1
		return 1 - (bodyState.Weapons_numWeaponsCharged() / 3);
	}

	/// <summary>
	/// High score for target having high heat, low score for target having low heat
	/// </summary>
	/// <returns>float between 0 and 1</returns>
	private float Target_HeatConsideration()
	{
		if (bodyState.targetBodyState != null)
		{
			//Debug.Log(Mathf.Pow((bodyState.targetBodyState.HeatContainer_getCurrentHeat() - bodyState.heatContainer.GetAirTemperature()) / (bodyState.targetBodyState.cooling.GetMaxHeat() - bodyState.heatContainer.GetAirTemperature()), 8));
			return Mathf.Pow((bodyState.targetBodyState.HeatContainer_getCurrentHeat() - bodyState.heatContainer.GetAirTemperature()) / (bodyState.targetBodyState.cooling.GetMaxHeat() - bodyState.heatContainer.GetAirTemperature()), 8);
		}
		else
		{
			return 1f;
		}
	}

	/// <summary>
	/// Score to determine if siphon is deployed
	/// </summary>
	/// <returns>deployedVal if siphon is deployed, notDeployedVal if siphon not deployed</returns>
	private float DeployedSiphonConsideration(float deployedVal, float notDeployedVal)
	{
		return bodyState.Siphon_isExtended() ? deployedVal : notDeployedVal;
	}


	/// <summary>
	/// Determine if there is anything left to siphon 
	/// </summary>
	/// <returns>1 if there is still siphon left OR we haven't registered any siphon targets yet, 0 if we registered a siphon target and it has no dollars left</returns>
	private float SiphonAmountLeftConsideration()
	{
		return bodyState.siphon.siphonTarget != null ? bodyState.siphon.siphonTarget.dollarsLeft > 0 ? 1 : 0 : 1;
	}

	/// <summary>
	/// Score to determine if we have seen a hostile yet
	/// </summary>
	/// <returns>low value if we are not aware of a hostile, 1 if we are aware</returns>
	private float AwareOfHostileConsideration()
	{
		if (bodyState.targetBodyState != null)
		{
			return 1f;
		}
		else
		{
			return 0.2f;
		}
	}

	#endregion
}