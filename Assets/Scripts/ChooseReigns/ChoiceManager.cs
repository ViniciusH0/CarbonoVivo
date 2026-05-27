using System.Collections.Generic;
using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public List<bool> choices = new List<bool>();

    public void AddChoice(bool decision)
    {
        choices.Add(decision);

        foreach (var c in choices)
        {
            Debug.Log(c ? "SIM" : "NÃO");
        }
    }
}