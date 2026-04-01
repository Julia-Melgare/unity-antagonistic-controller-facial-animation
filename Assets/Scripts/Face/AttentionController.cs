using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttentionController : MonoBehaviour
{
   public abstract FixationObject GetCurrentFocus();
   public abstract float GetCurrentFixationTime();
}
