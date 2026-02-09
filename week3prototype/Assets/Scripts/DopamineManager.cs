using System.Collections;
using Mechanics;
using Microlight.MicroBar;
using UnityEngine;

public class DopamineManager : MonoBehaviour
{
    public static DopamineManager Instance;

    [SerializeField] MicroBar dopBar;
    
    [SerializeField] bool constantDecOn = false;
    [SerializeField] float amtToConstDec = 1f;
    
    [SerializeField] float goldAmt = 50f;
    [SerializeField] float posAmt = 10f;
    [SerializeField] float negAmt = -10f;
    [SerializeField] float neutralAmt = 10f;
    //private float crRunning = false; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
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
            DecreaseDopSlightly(amtToConstDec); 
        }
    }

    public void DecreaseDopSlightly(float amount)
    {
        dopBar.UpdateBar(dopBar.CurrentValue - amount); 
    }

    public void TrackDopEffects(PostInfo.PostType type)
    {
        constantDecOn = false; 
        switch (type)
        {
            case PostInfo.PostType.Gold:
                changeDop(goldAmt);
                break;
            case PostInfo.PostType.Positive:
                changeDop(posAmt);
                break;
            case PostInfo.PostType.Negative:
                changeDop(negAmt);
                break;
            case PostInfo.PostType.Neutral:
                changeDop(neutralAmt); 
                break;
        }
        constantDecOn = true; 

        //OnStatsChanged?.Invoke(); // Notify UI
                                      
        
    }

    public void changeDop(float amount)
    {
        dopBar.UpdateBar(dopBar.CurrentValue + amount);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
