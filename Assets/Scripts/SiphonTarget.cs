using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SiphonTarget : MonoBehaviour
{
	public float dollarsPerSecond = 1;
	public float dollarAmount = 100;
	public float dollarsLeft = 1;
	public SiphonModel siphoner;

	public GameObject dollarsLeftIndicator;

	public Material targeted;
	public Material idle;
	Vector3 scaleCache;

	// Start is called before the first frame update
	void Start()
	{
		dollarsLeft = dollarAmount;
		scaleCache = dollarsLeftIndicator.transform.localScale;
	}

	// Update is called once per frame
	void Update()
	{
		if (siphoner != null)
		{
			if (dollarsLeft > 0)
			{
				siphoner.addDollars(Time.deltaTime * dollarsPerSecond * siphoner.getSiphoningRate());
				dollarsLeft -= Time.deltaTime * dollarsPerSecond * siphoner.getSiphoningRate();
				dollarsLeftIndicator.transform.localScale = scaleCache * (dollarsLeft / dollarAmount);
				//Debug.Log(siphoner.dollars);
			}
		}
	}

	public void beingSiphoned()
	{
		//this.GetComponent<MeshRenderer>().material = targeted;
		dollarsLeftIndicator.GetComponent<MeshRenderer>().material = targeted;
	}

	public void notBeingSiphoned()
	{
		this.GetComponent<MeshRenderer>().material = idle;
		dollarsLeftIndicator.GetComponent<MeshRenderer>().material = idle;
	}
}
