using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    public string SpecialAnimation;
    public float AnimationFrequency;
    private float animInterval;
    private float animTimer;    

    public string IdleAnimation;
    void Start()
    {
        animInterval = 1.0f / AnimationFrequency;
        animator.Play(IdleAnimation);
    }

    // Update is called once per frame
    void Update()
    {
        animTimer -= Time.deltaTime;

        if (SpecialAnimation != "" && animTimer < 0)
        {
            animTimer += animInterval;
            PlayAnimation(SpecialAnimation);
        }
    }

    void PlayAnimation(string animationClip)
    {
        animator.Play(animationClip);
    }
}
