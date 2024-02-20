using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Env : MonoBehaviour
{
    public abstract void OnEpisodeBegin();
    public abstract void OnEpisodeEnd();
}
