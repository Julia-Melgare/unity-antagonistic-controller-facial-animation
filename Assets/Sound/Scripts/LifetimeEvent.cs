using UnityEngine;

public class LifetimeEvent : MonoBehaviour
{

	//public AK.Wwise.Event soundEvent;

    private uint soundID;

	void Start()
    {
		  //soundID = soundEvent.Post(gameObject);
    }

    void OnDestroy()
    {
		  //AkSoundEngine.StopPlayingID(soundID);
	  }
}
