using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BodyState : MonoBehaviour
{
	public CoolingModel cooling;
	private LegsModel legs;
	private SensorsModel sensors;
	public WeaponsModel weapons;
	private SiphonModel siphon;
	public Rigidbody rb;

	public float bodyHeat;
	public bool bodyIsOverheated;

	public Collider head;

	public BodyState targetBodyState;

	public void Init(List<SystemModel> systems)
	{

		cooling = systems.OfType<CoolingModel>().FirstOrDefault();
		legs = systems.OfType<LegsModel>().FirstOrDefault();
		sensors = systems.OfType<SensorsModel>().FirstOrDefault();
		weapons = systems.OfType<WeaponsModel>().FirstOrDefault();
		siphon = systems.OfType<SiphonModel>().FirstOrDefault();
	}

	void Update()
	{
		bodyHeat = cooling.currentHeat;
		bodyIsOverheated = cooling.isOverheated;
	}

	public int Cooling_getSystemHealth()
	{
		return cooling.currentLevel;
	}

	public float Cooling_getCurrentHeat()
	{
		return cooling.currentHeat;
	}

	public bool Cooling_IsOverheated()
	{
		return cooling.isOverheated;
	}

	public int Legs_getSystemHealth()
	{
		return legs.currentLevel;
	}

	public int Weapons_getSystemHealth()
	{
		return weapons.currentLevel;
	}

	public bool Weapons_weapon1Powered()
	{
		return weapons.GetCurrentPowerAllocationDictionary()[0];
	}

	public bool Weapons_weapon1Charged()
	{
		return weapons.guns[0].isCharged();
	}

	public bool Weapons_weapon2Powered()
	{
		return weapons.GetCurrentPowerAllocationDictionary()[1];
	}
	public bool Weapons_weapon2Charged()
	{
		return weapons.guns[1].isCharged();
	}

	public bool Weapons_weapon3Powered()
	{
		return weapons.GetCurrentPowerAllocationDictionary()[2];
	}
	public bool Weapons_weapon3Charged()
	{
		return weapons.guns[2].isCharged();
	}

	public bool Weapons_noWeaponsCharged()
	{
		return !(Weapons_weapon1Charged() || Weapons_weapon2Charged() || Weapons_weapon3Charged());
	}

	public int Weapons_numWeaponsCharged()
	{
		int n = 0;
		if (weapons.guns[0].isCharged())
		{
			n++;
		}

		if (weapons.guns[1].isCharged())
		{
			n++;
		}

		if (weapons.guns[2].isCharged())
		{
			n++;
		}

		return n;
	}

	public bool[] Weapons_currentWeaponsCharged()
	{
		return new bool[] { weapons.guns[0].isCharged(), weapons.guns[1].isCharged(), weapons.guns[2].isCharged() };
	}

	public bool[] Weapons_currentWeaponsPowered()
	{
		return weapons.GetCurrentPowerAllocationDictionary();
	}

	public bool Weapons_currentlyFiringBurst()
	{
		return weapons.guns[0].isFiringBurst || weapons.guns[1].isFiringBurst || weapons.guns[2].isFiringBurst;
	}

	public int Sensors_getSystemHealth()
	{
		return sensors.currentLevel;
	}

	public int Siphon_getSystemHealth()
	{
		return siphon.currentLevel;
	}

	public bool Siphon_isExtended()
	{
		return siphon.extended;
	}
}