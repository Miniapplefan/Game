using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class GunSelector : MonoBehaviour
{
	[SerializeField]
	private GunType Gun1;
	[SerializeField]
	private GameObject Gun1Parent;
	[SerializeField]
	private GunType Gun2;
	[SerializeField]
	private GameObject Gun2Parent;
	[SerializeField]
	private GunType Gun3;
	[SerializeField]
	private GameObject Gun3Parent;
	[SerializeField]
	private List<GunDataScriptableObject> Guns;
	[SerializeField]
	private Rigidbody weapon;

	[Space]
	[Header("Runtime Filled")]
	public Gun ActiveGun1;
	public Gun ActiveGun2;
	public Gun ActiveGun3;

	public LayerMask raycastLayerMask;
	private float lastRaycastTime;
	private float raycastInterval = 0.5f; // Adjust this value as needed

	public Transform[] gunHolders;

	private void Start()
	{
		ActiveGun1 = CreateGun(Gun1, Gun1Parent);
		ActiveGun2 = CreateGun(Gun2, Gun2Parent);
		ActiveGun3 = CreateGun(Gun3, Gun3Parent);
		Debug.Log("created guns");
	}

	void Update()
	{
		if (Time.time - lastRaycastTime >= raycastInterval)
		{
			PerformRaycast();
			lastRaycastTime = Time.time;
		}
	}

	void PerformRaycast()
	{
		RaycastHit hit;
		if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, raycastLayerMask))
		{
			// Rotate the guns to look at the hit point
			RotateGuns(hit.point);
		}
		else
		{
			// No hit, keep the previous rotation
		}
	}

	void RotateGuns(Vector3 targetPoint)
	{
		foreach (Transform gun in gunHolders)
		{
			Vector3 targetDirection = targetPoint - gun.position;
			Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
			gun.rotation = targetRotation;
		}
	}

	private Gun CreateGun(GunType type, GameObject slot)
	{

		//Guns[index].Spawn(slot, this);
		//return Guns[index];
		GunDataScriptableObject gunData = Guns.Find(gun => gun.type == type);

		//if (gun == null)
		//{
		//    Debug.LogError($"No GunScriptableObject found for GunType: {gun}");
		//    return null;
		//}

		//gun.Spawn(slot, this);
		//return gun;

		GameObject gunObject = new GameObject(gunData.GunName);
		Gun gun = gunObject.AddComponent<Gun>();
		gun.gunData = gunData;
		gun.SetParent(slot, weapon);
		return gun;
	}

	void OnDrawGizmos()
	{
		Color color;
		color = Color.green;
		DrawHelperAtCenter((weapon.transform.up + -(weapon.transform.right * 0.5f)), color, 2f);

	}
	private void DrawHelperAtCenter(
								 Vector3 direction, Color color, float scale)
	{
		Gizmos.color = color;
		Vector3 destination = transform.position + direction * scale;
		Gizmos.DrawLine(transform.position, destination);
	}
}

