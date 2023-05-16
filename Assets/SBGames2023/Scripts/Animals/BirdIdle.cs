using System.Collections;
using Cinemachine;
using UnityEngine;

public class BirdIdle : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(StartIdle());
    }

    IEnumerator StartIdle()
    {
        float offset = Random.Range(0, 1f);
        yield return new WaitForSeconds(3f + offset);
        animator.Play("Idle_B");
    }
}
