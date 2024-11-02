using UnityEngine;

[CreateAssetMenu(menuName = "AI/Attack Config", fileName = "Attack Config", order = 1)]
public class AttackConfigSO : ScriptableObject
{
	public int SensorRadius = 40;
	public float FOVAngle = 90;
	public LayerMask AttackableLayerMask;
	public LayerMask EnvironmentalCoolingLayerMask;

	public LayerMask SiphonableLayerMask;
	[Tooltip("Lower is a better hiding spot")]
	public float HideSensitivity = 0;
	public float MinPlayerDistance = 5f;
	[Range(0, 5f)]
	public float MinObstacleHeight = 1.25f;
	public LayerMask ObstructionLayerMask;
	public float AttackDelay = 1;
	public int AttackCost = 4;
	public float SiphonDelay = 1;

}