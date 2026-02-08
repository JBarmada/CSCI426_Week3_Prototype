using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; 
public class HealthController : MonoBehaviour
{
    private Image _image;
    //public ScrollMechanic scrollMechanic;
    //[SerializeField] float maxSpeed = 30f; //max speed of the scroll 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _image = GetComponent<Image>();

    }

    // function to change the appearance of the health bar 
    public void UpdateHealthBar(GameObject healthbar, float maxHealth, float currentHealth)
    {
        //amount which the bar is filled can be represented as a proportion of the 'current health' over the 'max health'
        //_image.fillAmount = currentHealth / maxHealth; 
        //Image _image = healthbar.GetComponentInChildren<Image>();
        //if(_image == null)
        //{
        //    Debug.Log("Could not find image");
        //}
        _image.fillAmount = currentHealth / maxHealth;
    }

    private void Update()
    {
        //if (scrollMechanic == null)
        //{
        //    Debug.LogError("ScrollMechanic reference is not assigned in the inspector.");
        //}

        ////get the current speed of the scroll 
        //float currVelocity = scrollMechanic.Inertia; 
        //float currSpeed = Mathf.Abs(currVelocity);
        //UpdateHealthBar(maxSpeed, currSpeed);

    }
}
