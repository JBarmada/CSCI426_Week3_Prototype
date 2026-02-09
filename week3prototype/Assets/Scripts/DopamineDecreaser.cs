using System.Collections;
using Microlight.MicroBar;
using UnityEngine;

public class DopamineDecreaser : MonoBehaviour
{
    [SerializeField] MicroBar dopBar;
    [SerializeField] bool constantDecOn = false;
    [SerializeField] float amtToDec = 1f; 
    //private float crRunning = false; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //instantiate microbar with max health 
        dopBar.Initialize(100f);

        StartCoroutine(ConstantlyDecrease()); 
    }

    //coroutine to constantly decrease dopamine by a small amount 
    IEnumerator ConstantlyDecrease()
    {
        //crRunning = true; 
        while(constantDecOn)
        {
            yield return new WaitForSeconds(1f);
            DecreaseDopSlightly(amtToDec); 
        }

    }

    public void DecreaseDopSlightly(float amount)
    {
        dopBar.UpdateBar(dopBar.CurrentValue - amount); 
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
