using UnityEngine;

public class DuelMaster : Env
{

    public HumanBotAgent bot1;
    public HumanBotAgent bot2;

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        
    }

    public override void OnEpisodeBegin()
    {

    }

    public override void OnEpisodeEnd()
    {

    }

    public HumanBotAgent Other(HumanBotAgent agent)
    {
        return agent == bot1 ? bot2 : bot1;
    }

    public void Victory(HumanBotAgent winner)
    {
        HumanBotAgent loser = Other(winner);

        //loser.Kill(false);

    }

    public void Defeat(HumanBotAgent loser)
    {
        HumanBotAgent winner = Other(loser);

        winner.AddReward(1000);
        winner.EndEpisode();

    }

}
