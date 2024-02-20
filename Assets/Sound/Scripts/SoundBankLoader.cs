using UnityEngine;
using UnityEngine.Assertions;

public class SoundBankLoader
{
	[RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
		var eResult = AkSoundEngine.LoadBank("SoundBank.bnk", out uint _);
		Assert.AreEqual(eResult, AKRESULT.AK_Success);
	}
}
