using System.Collections;
using UnityEngine;

public class AutoClicker : MonoBehaviour
{
    public static float coProduction = 1f; 
    private void Awake()
    {
        StartCoroutine(AutoClick());
    }
    IEnumerator AutoClick()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            Co2.AddCo2(coProduction);
        }
    }
}
