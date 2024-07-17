using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegsModel : SystemModel
{
	public Rigidbody rb;

	public float moveAcceleration = 1000f;
	public float moveDeacceleration = 0.2f;
	private float moveDeaccelerationX;
	private float moveDeaccelerationZ;
	public float baseWalkSpeed = 4f;
	public bool canMove = true;

	public ICommand forwardCommand;
	public ICommand backwardCommand;
	public ICommand leftCommand;
	public ICommand rightCommand;

	public LegsModel(int currentLvl, Rigidbody r) :
	base(currentLvl)
	{
		rb = r;
		rightLegHealth = currentLvl;
		leftLegHealth = currentLvl;
		moveSpeed = getMoveSpeed();
	}

	protected override void InitCommands()
	{
		forwardCommand = new MoveForwardCommand(this);
		backwardCommand = new MoveBackwardCommand(this);
		leftCommand = new MoveLeftCommand(this);
		rightCommand = new MoveRightCommand(this);
	}

	public override void SetNameAndMaxLevel()
	{
		name = BodyInfo.systemID.Legs;
		maxLevel = 4;
	}

	public override void UpgradeLevel(int amount)
	{
		base.UpgradeLevel(amount);
		rightLegHealth = Mathf.Clamp(rightLegHealth + amount, currentLevelWithoutDamage, maxLevel);
		leftLegHealth = Mathf.Clamp(leftLegHealth + amount, currentLevelWithoutDamage, maxLevel); ;
	}

	float moveSpeed;

	int rightLegHealth;
	int leftLegHealth;

	float getSpeedFromLeg(int legHealth)
	{
		switch (legHealth)
		{
			case 0:
				return 0f;
			case 1:
				return 0.5f;
			case 2:
				return 0.6f;
			case 3:
				return 0.8f;
			case 4:
				return 1.0f;
			default:
				return 0f;
		}
	}

	public float getMoveSpeed()
	{
		if (canMove)
		{
			return getSpeedFromLeg(rightLegHealth) + getSpeedFromLeg(leftLegHealth);
		}
		else
		{
			return 0f;
		}
	}

	public void damageLeftLeg(int amount)
	{
		leftLegHealth = Mathf.Clamp(leftLegHealth - amount, 0, leftLegHealth);
	}

	public void damageRightLeg(int amount)
	{
		rightLegHealth = Mathf.Clamp(rightLegHealth - amount, 0, rightLegHealth);
	}

	public void healLeftLeg(int amount)
	{
		leftLegHealth = Mathf.Clamp(leftLegHealth + amount, leftLegHealth, currentLevel);
	}

	public void healRightLeg(int amount)
	{
		rightLegHealth = Mathf.Clamp(rightLegHealth + amount, rightLegHealth, currentLevel);
	}

	public bool isCurrentVelocityLessThanMax()
	{
		Vector2 horizontalMovement = new Vector2(rb.velocity.x, rb.velocity.z);
		return horizontalMovement.magnitude < baseWalkSpeed * getMoveSpeed();
	}

	public void DoMoveDeacceleration()
	{
		float wdx = Mathf.SmoothDamp(rb.velocity.x, 0, ref moveDeaccelerationX, moveDeacceleration);
		float wdz = Mathf.SmoothDamp(rb.velocity.z, 0, ref moveDeaccelerationZ, moveDeacceleration);
		rb.velocity = new Vector3(wdx, rb.velocity.y, wdz);
	}

	public void OnCoolingSystemOverheat()
	{
		canMove = false;
	}

	public void OnCoolingSystemCooledOff()
	{
		canMove = true;
	}

	#region Execute Commands

	public void ExecuteForward()
	{
		if (canMove)
		{
			forwardCommand.Execute();

		}
	}

	public void ExecuteBackward()
	{
		if (canMove)
		{
			backwardCommand.Execute();
		}
	}

	public void ExecuteLeft()
	{
		if (canMove)
		{
			leftCommand.Execute();
		}
	}

	public void ExecuteRight()
	{
		if (canMove)
		{
			rightCommand.Execute();
		}
	}

	#endregion
}
