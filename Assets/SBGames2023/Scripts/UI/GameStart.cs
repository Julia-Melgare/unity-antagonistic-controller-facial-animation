using UnityEngine;

public class GameStart : MonoBehaviour
{
    [SerializeField]
    private Canvas gameStartUICanvas;
    private bool gameStarted = false;
    void Start()
    {
        Time.timeScale = 0f;
    }

    void Update()
    {
        if(!gameStarted && Input.GetKey(KeyCode.Return))
        {
            Time.timeScale = 1.5f;
            gameStartUICanvas.enabled = false;
            gameStarted = true;
        }

    }
}
