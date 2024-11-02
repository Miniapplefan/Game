using UnityEngine;
using System;
using System.Collections.Generic;

public class HeatContainer : MonoBehaviour
{
	public enum ContainerType { Water, Air, Mech }
	public ContainerType containerType;

	public HeatMaterialScriptableObject heatMat;

	public float currentTemperature = 0f; // Current heat level
	public float maxTemperature = 10f; // Max heat capacity
	public float specificHeatCapacity;     // Specific heat capacity, different for each type
	public float mass = 1f;                // Mass of the object (could be volume for air/water)
	public float fluidInteractionConstant;
	public float thermalConductivity;
	public HeatContainer currentAir;
	public float ambientTemperature = 0f; // Ambient temp for dissipation
	public float dissipationRateFromAirCurrents = 1f; // Dissipation rate from air currents 
	public float dissipationRate = 1.0f; // Dissipation rate (passive or for heat transfer)
	public CoolingModel coolingModel; // Cooling model for mechs
	public bool isInTransferZone = false; // Tracks if this object is in a heat transfer zone
	public event Action OnOverheated; // Overheat event (for mechs)

	private List<HeatContainer> transferTargets = new List<HeatContainer>(); // For multi-way transfer

	float coolingConstant = 0.02f;

	void Awake()
	{
		InitFromHeatMaterialSO();
	}

	// Method to set specific heat capacity based on container type
	private void InitFromHeatMaterialSO()
	{
		containerType = heatMat.containerType;
		specificHeatCapacity = heatMat.specificHeatCapacity;
		//mass = heatMat.mass;
		fluidInteractionConstant = heatMat.fluidInteractionConstant;
		thermalConductivity = heatMat.thermalConductivity;
	}

	void Start()
	{
		// If a mech with a cooling model, initialize
		if (coolingModel != null)
		{
			maxTemperature = coolingModel.GetMaxHeat();
			coolingModel.OnCoolingStateChanged += OnCoolingStateChanged;
			OnCoolingStateChanged(coolingModel.currentCoolingState); // Initialize dissipation rate
		}
		else
		{
			CalculateMass();
		}
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5f);  // Radius can be adjusted
		foreach (var hitCollider in hitColliders)
		{
			HeatContainer otherHeatContainer = hitCollider.GetComponent<HeatContainer>();
			if (otherHeatContainer != null && !transferTargets.Contains(otherHeatContainer))
			{
				transferTargets.Add(otherHeatContainer);
				if (otherHeatContainer.containerType == ContainerType.Air)
				{
					currentAir = otherHeatContainer;
				}
				// if (containerType == ContainerType.Water)
				// {
				// 	Debug.Log("I, " + containerType + " am now exchanging with " + otherHeatContainer.containerType);
				// }
			}
		}
	}

	// Calculate max heat capacity based on volume of collider (optional for different container types)
	void CalculateMass()
	{
		Collider heatCollider = GetComponent<Collider>();
		if (heatCollider != null)
		{
			float volume = GetColliderVolume(heatCollider);
			mass = volume * heatMat.mass; // Adjust scale for heat capacity
																		// Debug.Log("Calculated mass for: " + heatMat.containerType + " = " + mass);

		}
	}

	void FixedUpdate()
	{
		//DissipateHeat();
		foreach (HeatContainer target in transferTargets)
		{
			TransferHeat(target);
		} // Always transfer heat if there are targets
	}

	// Called when cooling state changes (for mechs)
	private void OnCoolingStateChanged(CoolingModel.CoolingState state)
	{
		if (coolingModel != null)
		{
			switch (state)
			{
				case CoolingModel.CoolingState.PassiveCooldown:
					dissipationRate = 1;
					break;
				case CoolingModel.CoolingState.Cooldown:
					dissipationRate = coolingModel.GetCooldownMultiplier();
					break;
				case CoolingModel.CoolingState.CooldownOverheated:
					dissipationRate = coolingModel.GetOverheatedCoolingMultiplier();
					break;
				default:
					dissipationRate = 1;
					break;
			}
		}
	}

	// Newton's Law of Cooling
	float GetPassiveCoolingRate()
	{
		//float temperatureDifference = currentHeat - ambientTemperature;
		//float coolingConstant = 0.02f; // Tune this value for desired cooling effect
		//return coolingConstant * temperatureDifference;
		return 0;
	}

	// Method to calculate temperature from current heat
	public float GetTemperature()
	{
		return currentTemperature;
	}

	// Dissipate heat into the environment or air
	// void DissipateHeat()
	// {
	// 	//Debug.Log(dissipationRate);

	// 	if (currentTemperature > ambientTemperature)
	// 	{

	// 		float totalCoolingRate = 0;

	// 		// If mech, also apply active cooling
	// 		if (coolingModel != null)
	// 		{
	// 			totalCoolingRate += dissipationRate;
	// 		}

	// 		// Apply cooling and clamp to ambient temperature
	// 		currentHeat -= totalCoolingRate * Time.deltaTime;
	// 		currentHeat = Mathf.Max(currentHeat, ambientTemperature);
	// 	}

	// 	// Handle overheating for mechs
	// 	if (coolingModel != null)
	// 	{
	// 		coolingModel.HandleHeatExtremes(currentHeat, maxHeatCapacity);
	// 	}
	// }

	// Handles multi-way heat transfer
	public void TransferHeat(HeatContainer otherContainer)
	{
		if (containerType == ContainerType.Air || otherContainer.containerType == ContainerType.Air)
		{
			// Use Newton's Law of Cooling for the air
			ApplyNewtonsLawOfCooling(otherContainer);
		}
		else
		{
			// Use the conduction model for heat transfer between mechs and water
			ApplyConductionModel(otherContainer);
		}
	}

	private void ApplyNewtonsLawOfCooling(HeatContainer otherContainer)
	{
		// Ensure only one of them is Air (because Newton's law applies between a body and ambient air)
		if (containerType == ContainerType.Air || otherContainer.containerType == ContainerType.Air)
		{
			HeatContainer airContainer = (containerType == ContainerType.Air) ? this : otherContainer;
			HeatContainer bodyContainer = (containerType == ContainerType.Air) ? otherContainer : this;

			// Get the temperatures of both containers
			float airTemperature = airContainer.GetTemperature();
			float bodyTemperature = bodyContainer.GetTemperature();

			// Calculate the temperature difference
			float temperatureDifference = bodyTemperature - airTemperature;

			// Mech overheating case
			if (coolingModel != null && coolingModel.isOverheated)
			{
				// Mech is overheated and should dissipate heat below ambient temperature until it reaches its minimumTemperature
				float minTemperature = coolingModel.minimumTemperature;
				//temperatureDifference = bodyTemperature - Mathf.Min(airTemperature, minTemperature); // Allow cooling down to minTemperature

				// Apply Newton's law: heatTransfer = coolingConstant * (temp difference) * dissipationRate * Time.deltaTime
				float heatTransfer = GetCoolingConstant(airContainer) * (dissipationRate) * Time.deltaTime;

				// Calculate the temperature change for the body and air based on their specific heat capacity and mass
				float bodyTempChange = heatTransfer / (bodyContainer.mass * bodyContainer.specificHeatCapacity);
				float airTempChange = heatTransfer / (airContainer.mass * airContainer.specificHeatCapacity);

				// Update temperatures
				bodyContainer.currentTemperature -= bodyTempChange;
				airContainer.currentTemperature += airTempChange;

				// Clamp the body temperature to the minimumTemperature to prevent overcooling below the limit
				currentTemperature = Mathf.Max(bodyContainer.currentTemperature, minTemperature);

			}
			else
			{
				if (Mathf.Abs(temperatureDifference) > 0.01f)  // Only transfer heat if there's a significant difference
				{
					// Determine which way the heat flows
					//dissipationRateFromAirCurrents
					float heatTransfer = GetCoolingConstant(airContainer) * dissipationRateFromAirCurrents * Mathf.Abs(temperatureDifference) * Time.deltaTime;

					//					Debug.Log(heatTransfer);

					// If the Mech is hotter than the Air, heat should flow from the Mech to the Air
					if (temperatureDifference > 0)
					{
						// Mechs have active cooling which allows them to dissipate heat faster into the air
						heatTransfer *= bodyContainer.dissipationRate;
						// Heat flows from Mech to Air
						float bodyTempChange = heatTransfer / (bodyContainer.mass * bodyContainer.specificHeatCapacity);
						float airTempChange = heatTransfer / (airContainer.mass * airContainer.specificHeatCapacity);

						// Update temperatures
						bodyContainer.currentTemperature -= bodyTempChange;
						airContainer.currentTemperature += airTempChange;
					}
					else
					{
						// Heat flows from Air to Mech (Air is hotter)
						float bodyTempChange = heatTransfer / (bodyContainer.mass * bodyContainer.specificHeatCapacity);
						float airTempChange = heatTransfer / (airContainer.mass * airContainer.specificHeatCapacity);

						// Update temperatures
						bodyContainer.currentTemperature += bodyTempChange;
						airContainer.currentTemperature -= airTempChange;
					}

					// Ensure temperatures remain within realistic bounds
					bodyContainer.currentTemperature = Mathf.Max(bodyContainer.currentTemperature, airContainer.ambientTemperature);  // Prevent Mechs from cooling below ambient unless overheated
					airContainer.currentTemperature = Mathf.Max(airContainer.currentTemperature, 0);  // Prevent Air from going below 0 (or any desired minimum)
				}
			}

			// Optionally handle any extreme heat conditions (such as additional cooling effects)
			if (coolingModel != null)
			{
				coolingModel.HandleHeatExtremes(bodyContainer.currentTemperature, bodyContainer.maxTemperature);
			}
		}
	}

	public float GetCoolingConstant(HeatContainer fluidContainer)
	{
		// Determine which fluid/material interaction is happening
		float interactionConstant = fluidContainer.fluidInteractionConstant;

		// Calculate coolingConstant based on thermal conductivity and interaction
		return interactionConstant * Mathf.Sqrt(thermalConductivity);
	}

	private void ApplyConductionModel(HeatContainer otherContainer)
	{
		float thisTemperature = GetTemperature();
		float otherTemperature = otherContainer.GetTemperature();

		// Calculate the temperature difference
		float temperatureDifference = Mathf.Abs(thisTemperature - otherTemperature);
		if (temperatureDifference > 1)  // Only proceed if there's a temperature difference
		{
			// Apply conduction heat transfer: heatTransfer = transferRate * (temp difference) * Time.deltaTime
			float heatTransfer = CalculateTransferRate(this, otherContainer) * temperatureDifference * Time.deltaTime;

			if (thisTemperature > otherTemperature)  // Transfer from this container to the other
			{
				float tempChangeThis = heatTransfer / (mass * specificHeatCapacity);
				float tempChangeOther = heatTransfer / (otherContainer.mass * otherContainer.specificHeatCapacity);

				// Update the temperatures of both containers
				currentTemperature -= tempChangeThis;
				otherContainer.currentTemperature += tempChangeOther;
			}
			else if (thisTemperature < otherTemperature)  // Transfer from the other container to this one
			{
				float tempChangeThis = heatTransfer / (mass * specificHeatCapacity);
				float tempChangeOther = heatTransfer / (otherContainer.mass * otherContainer.specificHeatCapacity);

				// Update the temperatures of both containers
				currentTemperature += tempChangeThis;
				otherContainer.currentTemperature -= tempChangeOther;
			}
			if (coolingModel != null)
			{
				coolingModel.HandleHeatExtremes(currentTemperature, maxTemperature);
			}
		}
	}

	private float CalculateTransferRate(HeatContainer container1, HeatContainer container2)
	{
		// Approximate contact area based on mass (simple assumption)
		//float contactArea = Mathf.Pow(container1.mass, 2.0f / 3.0f) + Mathf.Pow(container2.mass, 2.0f / 3.0f);

		// Calculate transfer rate based on thermal conductivity and contact area
		return (container1.thermalConductivity + container2.thermalConductivity) / 2.0f;
	}

	public float GetAirTemperature()
	{
		return currentAir != null ? currentAir.GetTemperature() : GetTemperature();
	}

	// Method to add a transfer target when entering heat transfer zone
	private void OnTriggerEnter(Collider other)
	{
		HeatContainer otherHeatContainer = other.GetComponent<HeatContainer>();
		if (otherHeatContainer != null && !transferTargets.Contains(otherHeatContainer))
		{
			transferTargets.Add(otherHeatContainer);
		}
	}

	// Method to remove transfer target when exiting heat transfer zone
	private void OnTriggerExit(Collider other)
	{
		HeatContainer otherHeatContainer = other.GetComponent<HeatContainer>();
		if (otherHeatContainer != null && transferTargets.Contains(otherHeatContainer))
		{
			transferTargets.Remove(otherHeatContainer);
		}
	}

	float GetColliderVolume(Collider collider)
	{
		if (collider is BoxCollider box)
		{
			return box.size.x * box.size.y * box.size.z;
		}
		else if (collider is SphereCollider sphere)
		{
			return (4f / 3f) * Mathf.PI * Mathf.Pow(sphere.radius, 3);
		}
		return 1f; // Default to 1 if no volume
	}

	// Increase heat method (for laser hits, etc.)
	public void IncreaseHeat(object sender, float amount)
	{
		float temperatureChange = amount / (mass * specificHeatCapacity);
		currentTemperature += temperatureChange;

		// Check for overheating based on temperature
		if (currentTemperature >= maxTemperature && OnOverheated != null)
		{
			OnOverheated?.Invoke();
		}
	}
}
