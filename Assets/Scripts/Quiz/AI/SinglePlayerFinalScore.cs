using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // For TextMeshPro
using UnityEngine.UI;

public class SinglePlayerFinalScore : MonoBehaviour
{
    [Header("UI Elements")]
    public Image playerAvatar; // Image to display Player's avatar
    public Image computerAvatar; // Image to display Computer's avatar
    public TextMeshProUGUI playerNameText; // Text to display Player's name
    public TextMeshProUGUI computerNameText; // Text to display Computer's name
    public TextMeshProUGUI scoreText; // Text to display the winner's score
    public TextMeshProUGUI totalTimeText; // Text to display the total time played
    public GameObject winStatusPlayer; // GameObject to show Player's win status
    public GameObject winStatusComputer; // GameObject to show Computer's win status

    [Header("Scene Settings")]
    public string sceneToLoadOnExit = "MainMenu"; // Scene to load when exiting

    public SinglePlayerQuizManagerWithAI quizManager; // Reference to the SinglePlayerQuizManagerWithAI script

    void OnEnable()
    {
        if (quizManager != null)
        {
            SetPlayerAvatar(quizManager.player1Avatar.sprite);
            SetPlayerName(quizManager.player1NameText.text);
            SetScores(quizManager.GetPlayer1Score(), quizManager.GetPlayer2Score());
            SetTotalTime(quizManager.GetTotalTime());
        }
        DisplayFinalScores();
    }

    void DisplayFinalScores()
    {
        // Display player avatar and name
        playerAvatar.sprite = quizManager.player1Avatar.sprite;
        playerNameText.text = quizManager.player1NameText.text;

        // Display computer avatar and name
        computerAvatar.sprite = Resources.Load<Sprite>("Avatars/ComputerAvatar");
        computerNameText.text = "Computer";

        Player winner = GetWinner();

        if (winner == Player.Player1 || winner == Player.Computer)
        {
            scoreText.text = $"Winner score: {GetWinnerScore(winner)}";

            if (winner == Player.Player1)
            {
                winStatusPlayer.SetActive(true);
                winStatusComputer.SetActive(false);
            }
            else
            {
                winStatusPlayer.SetActive(false);
                winStatusComputer.SetActive(true);
            }
        }
        else
        {
            scoreText.text = "No winner found.";
            winStatusPlayer.SetActive(false);
            winStatusComputer.SetActive(false);
        }

        // Display total time played
        totalTimeText.text = $"Total Time: {FormatTime(quizManager.GetTotalTime())}";
    }

    Player GetWinner()
    {
        if (quizManager.GetPlayer1Score() > quizManager.GetPlayer2Score())
        {
            return Player.Player1;
        }
        else if (quizManager.GetPlayer2Score() > quizManager.GetPlayer1Score())
        {
            return Player.Computer;
        }
        return Player.None;
    }

    int GetWinnerScore(Player winner)
    {
        if (winner == Player.Player1)
        {
            return quizManager.GetPlayer1Score();
        }
        else if (winner == Player.Computer)
        {
            return quizManager.GetPlayer2Score();
        }
        return 0;
    }

    public void ExitToScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnExit);
    }

    public void SetScores(int playerScore, int computerScore)
    {
        // No need to store these values as they are fetched directly from the quizManager
    }

    public void SetTotalTime(float totalTime)
    {
        // No need to store this value as it is fetched directly from the quizManager
    }

    public void SetPlayerAvatar(Sprite avatarSprite)
    {
        this.playerAvatar.sprite = avatarSprite;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerNameText.text = playerName;
    }

    private string FormatTime(float time)
    {
        int hours = Mathf.FloorToInt(time / 3600);
        int minutes = Mathf.FloorToInt((time % 3600) / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    private enum Player
    {
        None,
        Player1,
        Computer
    }
}
