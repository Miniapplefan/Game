using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveForwardCommand : ICommand
{
	LegsModel model;

	public MoveForwardCommand(LegsModel m)
	{
		model = m;
	}

	public void Execute()
	{
		//Debug.Log("move forward");
		model.rb.AddForce(model.rb.transform.forward * model.getMoveSpeed() * model.moveAcceleration * Time.deltaTime);
	}

}
