using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirCurrent : MonoBehaviour
{
	private AirCurrentGenerator generatorScript;  // Reference to the parent fan script

	public void Initialize(AirCurrentGenerator generator)
	{
		generatorScript = generator;
	}

	private void OnTriggerEnter(Collider other)
	{
		// Check if a HeatContainer has entered the cooling zone
		HeatContainer heatContainer = other.GetComponent<HeatContainer>();
		if (heatContainer != null)
		{
			generatorScript.OnHeatContainerEntered(heatContainer);
			//Debug.Log("Cooling " + heatContainer.gameObject.name);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		// Continuously check for obstructions and apply cooling effect if the path is clear
		HeatContainer heatContainer = other.GetComponent<HeatContainer>();
		if (heatContainer != null && generatorScript != null)
		{
			if (generatorScript.IsPathClear())
			{
				generatorScript.ApplyCoolingEffect(heatContainer);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		// Check if a HeatContainer has exited the cooling zone
		HeatContainer heatContainer = other.GetComponent<HeatContainer>();
		if (heatContainer != null)
		{
			generatorScript.OnHeatContainerExited(heatContainer);
		}
	}
}
