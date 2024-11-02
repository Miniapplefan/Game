using CrashKonijn.Goap.Behaviours;
using CrashKonijn.Goap.Classes.Builders;
using CrashKonijn.Goap.Configs.Interfaces;
using CrashKonijn.Goap.Enums;
using CrashKonijn.Goap.Resolver;
using UnityEngine;

[RequireComponent(typeof(DependencyInjector))]
public class GoapSetConfigFactory : GoapSetFactoryBase
{
	private DependencyInjector Injector;

	public override IGoapSetConfig Create()
	{
		GoapSetBuilder builder = new("NPCSet");
		Injector = GetComponent<DependencyInjector>();

		BuildGoals(builder);
		BuildActions(builder);
		BuildSensors(builder);

		return builder.Build();
	}

	private void BuildGoals(GoapSetBuilder builder)
	{
		builder.AddGoal<WanderGoal>()
		.AddCondition<IsWandering>(Comparison.GreaterThanOrEqual, 1);

		builder.AddGoal<OverheatHostileGoal>()
		.AddCondition<IsHostileTargetOverheated>(Comparison.GreaterThanOrEqual, 1);

		builder.AddGoal<CooldownGoal>()
		.AddCondition<IsOverheated>(Comparison.SmallerThanOrEqual, 0);

		builder.AddGoal<TakeCoverGoal>()
		.AddCondition<IsInHostileLineOfSight>(Comparison.SmallerThanOrEqual, 0);

		builder.AddGoal<DeploySiphonGoal>()
		.AddCondition<IsSiphonDeployed>(Comparison.GreaterThanOrEqual, 1);
	}

	private void BuildActions(GoapSetBuilder builder)
	{
		builder.AddAction<WanderAction>()
			.SetTarget<WanderTarget>()
			.AddEffect<IsWandering>(EffectType.Increase)
			.SetMoveMode(ActionMoveMode.MoveBeforePerforming)
			.SetBaseCost(5)
			.SetInRange(1);

		builder.AddAction<DeploySiphonAction>()
		.SetTarget<SiphonableTarget>()
		.AddEffect<IsSiphonDeployed>(EffectType.Increase)
		.SetBaseCost(3)
		.SetInRange(2);

		builder.AddAction<OverheatHostileAction>()
		 .SetTarget<HostileTarget>()
		 .AddEffect<IsHostileTargetOverheated>(EffectType.Increase)
		 .SetBaseCost(Injector.AttackConfig.AttackCost)
		 .SetMoveMode(ActionMoveMode.PerformWhileMoving)
		 .SetInRange(Injector.AttackConfig.SensorRadius);

		builder.AddAction<CooldownAction>()
		 .SetTarget<CooldownTarget>()
		 .AddEffect<IsOverheated>(EffectType.Decrease)
		 .SetBaseCost(2)
		 .SetInRange(40);

		builder.AddAction<TakeCoverAction>()
		.SetTarget<CoverTarget>()
		.AddEffect<IsInHostileLineOfSight>(EffectType.Decrease)
		.SetBaseCost(2)
		.SetInRange(40);
	}

	private void BuildSensors(GoapSetBuilder builder)
	{
		builder.AddTargetSensor<WanderTargetSensor>()
		.SetTarget<WanderTarget>();

		builder.AddTargetSensor<HostileTargetSensor>()
		.SetTarget<HostileTarget>();

		builder.AddTargetSensor<CooldownTargetSensor>()
		.SetTarget<CooldownTarget>();

		builder.AddTargetSensor<CoverTargetSensor>()
		.SetTarget<CoverTarget>();

		builder.AddTargetSensor<SiphonTargetSensor>()
		.SetTarget<SiphonableTarget>();

		builder.AddWorldSensor<HeatSensor>()
		.SetKey<IsOverheated>();

		builder.AddWorldSensor<HostileLineOfSightSensor>()
		.SetKey<IsInHostileLineOfSight>();

		builder.AddWorldSensor<SiphonDeployedSensor>()
		.SetKey<IsSiphonDeployed>();
	}
}