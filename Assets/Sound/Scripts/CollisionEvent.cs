using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionEvent : MonoBehaviour
{

	//public AK.Wwise.RTPC relativeVelocityRTPC;

	//public AK.Wwise.Event soundEvent;

	public void OnCollisionEnter(Collision collision)
    {
		//relativeVelocityRTPC.SetValue(gameObject, collision.relativeVelocity.magnitude);
		//soundEvent.Post(gameObject);
    }
}
