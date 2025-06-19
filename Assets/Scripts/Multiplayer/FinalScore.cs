using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // For TextMeshPro
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class FinalScore : MonoBehaviour
{
    [Header("UI Elements")]
    public Image avatar1; // Image to display Player 1's avatar
    public Image avatar2; // Image to display Player 2's avatar
    public TextMeshProUGUI player1NameText; // Text to display Player 1's name
    public TextMeshProUGUI player2NameText; // Text to display Player 2's name
    public TextMeshProUGUI scoreText; // Text to display the winner's score
    public GameObject winStatusPlayer1; // GameObject to show Player 1's win status
    public GameObject winStatusPlayer2; // GameObject to show Player 2's win status

    [Header("Scene Settings")]
    public string sceneToLoadOnExit = "MainMenu"; // Scene to load when exiting

    private const string AvatarKey = "Avatar"; // Custom Property key for avatars
    private const string PlayerNameKey = "PlayerName"; // Custom Property key for player name
    private const string ScoreKey = "Score"; // Custom Property key for score

    void OnEnable()
    {
        DisplayFinalScores();
    }

    void DisplayFinalScores()
    {
        Player player1 = PhotonNetwork.PlayerList[0];
        Player player2 = PhotonNetwork.PlayerList.Length > 1 ? PhotonNetwork.PlayerList[1] : null;

        if (player1 != null)
        {
            avatar1.sprite = GetAvatarSpriteFromCustomProperties(player1);
            player1NameText.text = player1.NickName;
        }

        if (player2 != null)
        {
            avatar2.sprite = GetAvatarSpriteFromCustomProperties(player2);
            player2NameText.text = player2.NickName;
        }

        Player winner = GetWinner();

        if (winner != null)
        {
            scoreText.text = $"Winner score: {GetWinnerScore(winner)}";

            if (winner == player1)
            {
                winStatusPlayer1.SetActive(true);
                winStatusPlayer2.SetActive(false);
            }
            else
            {
                winStatusPlayer1.SetActive(false);
                winStatusPlayer2.SetActive(true);
            }
        }
        else
        {
            scoreText.text = "No winner found.";
            winStatusPlayer1.SetActive(false);
            winStatusPlayer2.SetActive(false);
        }
    }

    Player GetWinner()
    {
        Player winner = null;
        int highestScore = 0;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(ScoreKey, out object scoreObj))
            {
                int score = (int)scoreObj;
                if (score > highestScore)
                {
                    highestScore = score;
                    winner = player;
                }
            }
        }

        return winner;
    }

    Sprite GetAvatarSpriteFromCustomProperties(Player player)
    {
        if (player.CustomProperties.TryGetValue(AvatarKey, out object avatar))
        {
            string avatarName = avatar as string;
            Sprite avatarSprite = Resources.Load<Sprite>($"Avatars/{avatarName}");
            if (avatarSprite == null)
            {
                Debug.LogError($"Avatar sprite not found for avatar name: {avatarName}");
            }
            return avatarSprite;
        }
        Debug.LogError("Avatar not found in custom properties.");
        return null; // Default sprite if not set
    }

    int GetWinnerScore(Player winner)
    {
        if (winner.CustomProperties.TryGetValue(ScoreKey, out object scoreObj))
        {
            return (int)scoreObj;
        }
        Debug.LogError("Score not found in custom properties.");
        return 0;
    }

    public void ExitToScene()
    {
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnExit);
    }
}