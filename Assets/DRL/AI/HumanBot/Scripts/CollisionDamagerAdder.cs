using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CollisionDamagerAdder : MonoBehaviour
{
    void Start()
    {
        foreach (var joint in GetComponent<HumanBotAgent>().joints)
        {
            joint.gameObject.AddComponent<CollisionDamager>();
        }
    }
}
