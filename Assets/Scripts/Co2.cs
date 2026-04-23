using UnityEngine;

public class Co2 : MonoBehaviour
{
   public static float co2Amount = 0f;


    public static void AddCo2(float amount)
    {
        co2Amount += amount;
    }
    public static void RemoveCo2(float amount)
    {
        co2Amount = Mathf.Max(0, co2Amount - amount);
    }
}
