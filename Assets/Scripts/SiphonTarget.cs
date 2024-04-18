using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SiphonTarget : MonoBehaviour
{
    public float dollarsPerSecond = 1;
    public float dollarAmount = 100;
    float dollarsLeft;
    public SiphonModel siphoner;

    public Material targeted;
    public Material idle;
    Vector3 scaleCache;

    // Start is called before the first frame update
    void Start()
    {
        dollarsLeft = dollarAmount;
        scaleCache = gameObject.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if(siphoner != null)
        {
            if(dollarsLeft > 0)
            {
                siphoner.addDollars(Time.deltaTime * dollarsPerSecond * siphoner.getSiphoningRate());
                dollarsLeft -= Time.deltaTime * dollarsPerSecond * siphoner.getSiphoningRate();
                gameObject.transform.localScale = scaleCache * (dollarsLeft / dollarAmount);
                Debug.Log(siphoner.dollars);
            }
        }
    }

    public void beingSiphoned()
    {
        this.GetComponent<MeshRenderer>().material = targeted;
    }

    public void notBeingSiphoned()
    {
        this.GetComponent<MeshRenderer>().material = idle;
    }
}
