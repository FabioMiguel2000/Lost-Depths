using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Oxygen : MonoBehaviour
{
    public float oxygenValue;
    public Slider oxygenSlider;
    public TMP_Text oxygenText;

    private Vector3 lastPosition;
    private Vector3 originalPosition;
    private float oxygenLostSpeed = 2f;
    private float oxygenRefillSpeed = 2.5f;

    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
        originalPosition = transform.position;
        oxygenSlider.maxValue = oxygenValue;
    }

    void Update()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        if (distanceMoved < 1)
        {
            oxygenValue -= distanceMoved * oxygenLostSpeed;
        }

        if (oxygenValue <= 0)
        {
            oxygenValue = 0;
            Die();
        }
       
        UpdateSlider(oxygenValue);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("OxygenStation"))
        {
            // if (Input.GetKeyDown(KeyCode.E))
            // {
                StartCoroutine("RefillOxygen");
                Debug.Log("Start refilling oxygen.");
            // }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("OxygenStation"))
        {
            StopCoroutine("RefillOxygen");
            Debug.Log("Stopped refilling oxygen.");
        }
    }

    void UpdateSlider(float value)
    {
        value = Mathf.Clamp(value, 0f, oxygenSlider.maxValue);
        oxygenSlider.value = value;
        oxygenText.text = "Oxygen: " + value.ToString("F0");
    }

    IEnumerator RefillOxygen()
    {
        while (oxygenValue < oxygenSlider.maxValue)
        {
            oxygenValue += oxygenRefillSpeed * Time.deltaTime;
            UpdateSlider(oxygenValue);
            yield return null;
        }
        Debug.Log("Oxygen refilled!");
    }


    void Die()
    {
        Debug.Log("Player has run out of oxygen");
        // Implement restart the level
        controller.enabled = false;
        transform.position = originalPosition;
        controller.enabled = true;
        oxygenValue = oxygenSlider.maxValue;
        UpdateSlider(oxygenValue);
    }

}
