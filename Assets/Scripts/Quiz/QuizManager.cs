using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro; // For TextMeshPro
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuizManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("UI Elements")]
    public GameObject answeringPanel; // Panel for the answering player
    public GameObject waitingPanel; // Panel for the waiting player
    public TextMeshProUGUI questionText; // Question Text
    public TextMeshProUGUI timerText; // Timer Display
    public TextMeshProUGUI messageText; // Message Display
    public TextMeshProUGUI player1ScoreText; // Player 1 Score Text
    public TextMeshProUGUI player2ScoreText; // Player 2 Score Text
    public TextMeshProUGUI totalTimeText; // Total Time Played
    public Button[] answerButtons; // Array of Answer Buttons
    public Button skipButton; // Button to skip the question
    public Button resetGameButton; // Button to reset the game
    public Button exitButton; // Button to exit to the reset scene
    public GameObject player1Shadow; // Shadow for Player 1
    public GameObject player2Shadow; // Shadow for Player 2
    public Image player1Avatar; // Avatar for Player 1
    public Image player2Avatar; // Avatar for Player 2
    public TextMeshProUGUI player1NameText; // Name Text for Player 1
    public TextMeshProUGUI player2NameText; // Name Text for Player 2
    public GameObject finalScorePanel; // Final score panel
    public GameObject quizPanel; // Quiz panel to deactivate
    public GameObject WaitingLobbyPanel;
    public GameObject playerLeftGameObject; // GameObject to show when a player leaves
    public GameObject outcomePanel; // Outcome panel
    public TextMeshProUGUI outcomeText; // Outcome Text
    public GameObject quizTaken; // Quiz taken object

    [Header("Quiz Data")]
    public LanguageProcesses[] languageProcesses; // Processes organized by language
    private int currentQuestionIndex = 0;
    private int questionsAnsweredInTurn = 0;
    private Dictionary<string, int> processQuestionTracker;

    [Header("Game Settings")]
    public int questionsPerProcess = 2; // Minimum questions per process per game
    public float timerDuration = 30f;
    public string selectedLanguage = "EN"; // Default language
    public int questionsBeforeSwitch = 2; // Questions before switching turn (adjustable in Editor)
    public int totalQuestions = 40; // Total questions before the game ends
    public float maxGameTime = 1200f; // Maximum game time in seconds (20 minutes by default)
    public string sceneToLoadOnReset = "MainMenu"; // Scene to load on game reset
    public float outcomeDisplayDuration = 4f; // Duration to display the outcome text

    [Header("Sounds")]
    public AudioSource buttonClickAudioSource; // AudioSource for button click sound
    public AudioSource turnChangeAudioSource; // AudioSource for turn change sound
    public AudioSource finalSecondsAudioSource; // AudioSource for final seconds sound

    [Header("Process Images")]
    public Image employeeOnboardingImage;
    public Image conflictResolutionImage;
    public Image trainingAndDevelopmentImage;
    public Image workplaceDiversityAndInclusionImage;
    public Image employeeEngagementImage;
    public Image teamCollaborationImage;

    [Header("Topic Scores")]
    public TextMeshProUGUI[] player1TopicScoreTexts; // Text assignments for each topic score for Player 1
    public TextMeshProUGUI[] player2TopicScoreTexts; // Text assignments for each topic score for Player 2

    [Header("Disconnection Settings")]
    public string sceneToLoadOnDisconnect = "MainMenu"; // Scene to load when a player disconnects

    [Header("Exit Buttons")]
    public Button quizPanelExitButton; // Button to exit from the quiz panel
    public Button waitingRoomExitButton; // Button to exit from the waiting room

    private float timer;
    private float totalTime = 0f; // Total time played
    private bool isPlayer1Turn = true;
    private bool isAnswering = false;
    private bool finalSecondsPlaying = false;

    private int player1Score = 0;
    private int player2Score = 0;
    private int totalQuestionsAnswered = 0; // Added definition for totalQuestionsAnswered

    private Dictionary<string, int> player1TopicScores;
    private Dictionary<string, int> player2TopicScores;

    private readonly string[] topics = {
        "Motivation & Leadership",
        "Empathy & Relationship-Building",
        "Communication Skills",
        "Self-Awareness & Self-Reflection",
        "Adaptability & Flexibility",
        "Conflict Management & Problem-Solving",
        "Cultural Intelligence & Diversity",
        "Emotional Regulation & Resilience",
        "Teamwork & Collaboration",
        "Critical Thinking",
        "Ethical decision-making",
        "Accountability & Responsibility",
        "Inclusion & Team Building",
        "Planning & Organization",
        "Strategic Problem-Solving",
        "Conflict Resolution",
        "Time Management",
        "Collaboration & Leadership",
        "Change Management",
        "Engagement Strategies",
        "Collaboration",
        "Emotional Regulation",
        "Empathy"
    };

    private const string AvatarKey = "Avatar"; // Custom Property key for avatars
    private const byte UpdateScoreEventCode = 2; // Custom Photon event code for updating scores

    private bool player1Finished = false;
    private bool player2Finished = false;

    private Coroutine turnCheckCoroutine;

    // Store original button image colors
    private Dictionary<Button, Color> originalButtonColors = new Dictionary<Button, Color>();

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            isPlayer1Turn = true;
        }

        // Initialize process tracker
        InitializeProcessTracker();

        // Initialize topic scores
        InitializeTopicScores();

        // Initialize UI and Timer
        timer = timerDuration;
        InitializeAvatarsAndNames();
        InitializePlayerNames(); // Initialize player names

        // Add skip button functionality
        skipButton.onClick.AddListener(SkipQuestion);
        resetGameButton.onClick.AddListener(ResetGame);
        exitButton.onClick.AddListener(ExitToResetScene); // Add exit button functionality

        // Assign exit button functionality
        quizPanelExitButton.onClick.AddListener(ExitToDisconnectScene);
        waitingRoomExitButton.onClick.AddListener(ExitToDisconnectScene);

        // Ensure correct initial panel visibility
        UpdatePanels();
        LoadQuestion();

        // Register Photon event
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;

        // Start the turn check coroutine
        turnCheckCoroutine = StartCoroutine(CheckTurnState());

        UpdatePlayerLanguages(); // Update player languages at the start
    }

    void OnDestroy()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;

        // Stop the turn check coroutine
        if (turnCheckCoroutine != null)
        {
            StopCoroutine(turnCheckCoroutine);
        }
    }

    void Update()
    {
        if (!finalScorePanel.activeSelf) // If the game is not over
        {
            if (!isAnswering)
            {
                timer -= Time.deltaTime;
                if (timer <= 10f && !finalSecondsPlaying)
                {
                    finalSecondsPlaying = true;
                    PlaySound(finalSecondsAudioSource, true); // Loop the final seconds sound
                }

                if (timer <= 0f)
                {
                    timer = 0f;
                    OnTimeOver();
                }
            }

            UpdateTotalTimeText();
            UpdateTimerUI();

            if (totalTime >= maxGameTime || totalQuestionsAnswered >= totalQuestions)
            {
                EndGame();
            }
        }

        UpdatePlayerLanguages(); // Continuously ensure the correct language is applied
    }

    IEnumerator CheckTurnState()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // Check every 5 seconds

            // Ensure the turn state is synchronized
            photonView.RPC("SyncTurnState", RpcTarget.All, isPlayer1Turn);
        }
    }

    [PunRPC]
    void SyncTurnState(bool currentTurn)
    {
        isPlayer1Turn = currentTurn;
        UpdatePanels();
    }

    void InitializeProcessTracker()
    {
        processQuestionTracker = new Dictionary<string, int>();
        foreach (var process in languageProcesses)
        {
            foreach (var proc in process.processes)
            {
                processQuestionTracker[proc.name] = 0;
            }
        }
    }

    void InitializeTopicScores()
    {
        player1TopicScores = new Dictionary<string, int>();
        player2TopicScores = new Dictionary<string, int>();

        foreach (var topic in topics)
        {
            player1TopicScores[topic] = 0;
            player2TopicScores[topic] = 0;
        }
    }

    void LoadQuestion()
    {
        if (isPlayer1Turn != PhotonNetwork.IsMasterClient) return; // Only load question for the player whose turn it is

        isAnswering = false; // Reset answering state
        finalSecondsPlaying = false; // Reset final seconds sound state
        StopSound(finalSecondsAudioSource); // Stop any looping sounds

        string currentLanguage = PhotonNetwork.IsMasterClient ? LanguageManager.GetPlayerLanguage(PhotonNetwork.PlayerList[0].ActorNumber) : LanguageManager.GetPlayerLanguage(PhotonNetwork.PlayerList[1].ActorNumber);
        selectedLanguage = currentLanguage; // Dynamically update the language
        LanguageProcesses languageProcess = System.Array.Find(languageProcesses, lang => lang.language == selectedLanguage);
        if (languageProcess == null)
        {
            Debug.LogError($"No data available for language: {selectedLanguage}");
            return;
        }

        foreach (var process in languageProcess.processes)
        {
            if (processQuestionTracker[process.name] < questionsPerProcess)
            {
                var question = process.GetNextQuestion();
                if (question == null)
                {
                    Debug.LogError($"No questions left in process: {process.name}");
                    continue;
                }

                processQuestionTracker[process.name]++;
                questionText.text = question.text;
                currentQuestionIndex++;

                for (int i = 0; i < answerButtons.Length; i++)
                {
                    // Check if there are enough answers for this button
                    if (i < question.answers.Length)
                    {
                        // Set button text
                        answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = question.answers[i];

                        // Clear any previous listener to avoid stacking
                        answerButtons[i].onClick.RemoveAllListeners();

                        // Add the correct listener dynamically
                        int answerIndex = i; // Capture the current index for closure
                        answerButtons[i].onClick.AddListener(() => CheckAnswer(answerIndex, question.correctAnswer, question.GetSelectedTopic(), question.outcome));
                        answerButtons[i].gameObject.SetActive(true);

                        // Store the original color if not already stored
                        var img = answerButtons[i].GetComponent<Image>();
                        if (img != null && !originalButtonColors.ContainsKey(answerButtons[i]))
                        {
                            originalButtonColors[answerButtons[i]] = img.color;
                        }
                    }
                    else
                    {
                        // Disable extra buttons
                        answerButtons[i].gameObject.SetActive(false);
                    }
                }

                // Reset timer and clear any old messages
                timer = timerDuration;
                messageText.text = "";
                UpdateTimerForAllPlayers(timer); // Update timer for all players

                // Update process image
                UpdateProcessImage(process.name);

                return;
            }
        }

        Debug.Log("All questions have been answered.");
        EndGame();
    }

    public void CheckAnswer(int chosenIndex, int correctIndex, string topic, string outcome)
    {
        if (isAnswering) return;

        isAnswering = true;

        Button selectedButton = answerButtons[chosenIndex];

        if (chosenIndex == correctIndex)
        {
            messageText.text = "Correct";
            HighlightButton(selectedButton, Color.green); // Highlight correct answer
            if (isPlayer1Turn)
            {
                player1Score++;
                player1TopicScores[topic]++;
            }
            else
            {
                player2Score++;
                player2TopicScores[topic]++;
            }
            UpdateScores();
        }
        else
        {
            messageText.text = "Wrong";
            HighlightButton(selectedButton, Color.red); // Highlight wrong answer
            HighlightButton(answerButtons[correctIndex], Color.green); // Highlight correct answer
        }

        questionsAnsweredInTurn++;
        totalQuestionsAnswered++;

        if (totalQuestionsAnswered >= totalQuestions)
        {
            if (isPlayer1Turn)
            {
                player1Finished = true;
            }
            else
            {
                player2Finished = true;
            }

            if (player1Finished && player2Finished)
            {
                photonView.RPC("ShowFinalScorePanel", RpcTarget.All);
            }
            else
            {
                photonView.RPC("ShowWaitingLobbyPanel", RpcTarget.All);
            }
        }
        else
        {
            StartCoroutine(ShowOutcomeAfterDelay(outcome));
        }
    }

    IEnumerator ShowOutcomeAfterDelay(string outcome)
    {
        yield return new WaitForSeconds(3f); // Wait for 3 seconds
        outcomeText.text = outcome;
        outcomePanel.SetActive(true);
        yield return new WaitForSeconds(outcomeDisplayDuration);
        outcomePanel.SetActive(false);

        ResetButtonColors(); // Reset button colors to default

        if (questionsAnsweredInTurn >= questionsBeforeSwitch)
        {
            questionsAnsweredInTurn = 0;
            photonView.RPC("SwitchTurnRPC", RpcTarget.All);
        }
        else
        {
            LoadQuestion();
        }
    }

    void HighlightButton(Button button, Color color)
    {
        var img = button.GetComponent<Image>();
        if (img != null)
        {
            // Store original color if not already stored (safety for any missed case)
            if (!originalButtonColors.ContainsKey(button))
                originalButtonColors[button] = img.color;
            img.color = color;
        }
    }

    void ResetButtonColors()
    {
        foreach (Button btn in answerButtons)
        {
            var img = btn.GetComponent<Image>();
            if (img != null && originalButtonColors.ContainsKey(btn))
            {
                img.color = originalButtonColors[btn];
            }
        }
    }

    [PunRPC]
    void SwitchTurnRPC()
    {
        isPlayer1Turn = !isPlayer1Turn;
        PlaySound(turnChangeAudioSource);
        UpdatePanels();

        // Reset the timer for the new turn and synchronize it
        photonView.RPC("ResetTimerRPC", RpcTarget.All, timerDuration);

        if (isPlayer1Turn == PhotonNetwork.IsMasterClient)
        {
            LoadQuestion();
        }
    }

    [PunRPC]
    void ResetTimerRPC(float newTimer)
    {
        timer = newTimer;
        finalSecondsPlaying = false; // Reset final seconds sound state
        StopSound(finalSecondsAudioSource); // Stop any looping sounds
        UpdateTimerUI();
    }

    void OnTimeOver()
    {
        messageText.text = "Time Over! Question Passed.";
        StopSound(finalSecondsAudioSource); // Stop any looping sounds
        questionsAnsweredInTurn++;
        totalQuestionsAnswered++;

        if (totalQuestionsAnswered >= totalQuestions)
        {
            if (isPlayer1Turn)
            {
                player1Finished = true;
            }
            else
            {
                player2Finished = true;
            }

            if (player1Finished && player2Finished)
            {
                photonView.RPC("ShowFinalScorePanel", RpcTarget.All);
            }
            else
            {
                photonView.RPC("ShowWaitingLobbyPanel", RpcTarget.All);
            }
        }
        else
        {
            if (questionsAnsweredInTurn >= questionsBeforeSwitch)
            {
                questionsAnsweredInTurn = 0;
                photonView.RPC("SwitchTurnRPC", RpcTarget.All);
            }
            else
            {
                LoadQuestion();
            }
        }
    }

    void SkipQuestion()
    {
        messageText.text = "Question Skipped!";
        StopSound(finalSecondsAudioSource); // Stop any looping sounds
        questionsAnsweredInTurn++;
        totalQuestionsAnswered++;

        if (totalQuestionsAnswered >= totalQuestions)
        {
            if (isPlayer1Turn)
            {
                player1Finished = true;
            }
            else
            {
                player2Finished = true;
            }

            if (player1Finished && player2Finished)
            {
                photonView.RPC("ShowFinalScorePanel", RpcTarget.All);
            }
            else
            {
                photonView.RPC("ShowWaitingLobbyPanel", RpcTarget.All);
            }
        }
        else
        {
            if (questionsAnsweredInTurn >= questionsBeforeSwitch)
            {
                questionsAnsweredInTurn = 0;
                photonView.RPC("SwitchTurnRPC", RpcTarget.All);
            }
            else
            {
                LoadQuestion();
            }
        }
    }

    void UpdateTimerUI()
    {
        timerText.text = $"Time Left: {Mathf.Ceil(timer)}";
    }

    void UpdateTotalTimeText()
    {
        int hours = Mathf.FloorToInt(totalTime / 3600);
        int minutes = Mathf.FloorToInt((totalTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(totalTime % 60);
        totalTimeText.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}sec";
    }

    void UpdateScores()
    {
        player1ScoreText.text = player1Score.ToString();
        player2ScoreText.text = player2Score.ToString();
        photonView.RPC("UpdateScoresRPC", RpcTarget.Others, player1Score, player2Score, player1TopicScores, player2TopicScores);
    }

    [PunRPC]
    void UpdateScoresRPC(int p1Score, int p2Score, Dictionary<string, int> p1TopicScores, Dictionary<string, int> p2TopicScores)
    {
        player1Score = p1Score;
        player2Score = p2Score;
        player1ScoreText.text = player1Score.ToString();
        player2ScoreText.text = player2Score.ToString();
        player1TopicScores = p1TopicScores;
        player2TopicScores = p2TopicScores;
        UpdateTopicScores();
    }

    void InitializePlayerNames()
    {
        player1NameText.text = PhotonNetwork.PlayerList[0].NickName;
        player2NameText.text = PhotonNetwork.PlayerList[1].NickName;
    }

    void InitializeAvatarsAndNames()
    {
        if (PhotonNetwork.PlayerList.Length > 0)
        {
            player1Avatar.sprite = GetAvatarSpriteFromCustomProperties(PhotonNetwork.PlayerList[0]);
            player1NameText.text = PhotonNetwork.PlayerList[0].NickName;

            if (PhotonNetwork.PlayerList.Length > 1)
            {
                player2Avatar.sprite = GetAvatarSpriteFromCustomProperties(PhotonNetwork.PlayerList[1]);
                player2NameText.text = PhotonNetwork.PlayerList[1].NickName;
            }
        }
    }

    Sprite GetAvatarSpriteFromCustomProperties(Player player)
    {
        if (player.CustomProperties.TryGetValue(AvatarKey, out object avatar))
        {
            string avatarName = avatar as string;
            return Resources.Load<Sprite>($"Avatars/{avatarName}"); // Ensure sprites are stored in a Resources/Avatars folder
        }
        return null; // Default sprite if not set
    }

    void PlaySound(AudioSource audioSource, bool loop = false)
    {
        if (audioSource)
        {
            audioSource.loop = loop;
            audioSource.Play();
        }
    }

    void StopSound(AudioSource audioSource)
    {
        if (audioSource)
        {
            audioSource.loop = false;
            audioSource.Stop();
        }
    }

    [PunRPC]
    void ShowFinalScorePanel()
    {
        finalScorePanel.SetActive(true);
        quizPanel.SetActive(false); // Deactivate the quiz panel
        WaitingLobbyPanel.SetActive(false); 
        StopSound(finalSecondsAudioSource); // Stop any ongoing sounds

        // Update topic scores in the final score panel
        UpdateTopicScores();
    }

    [PunRPC]
    void ShowWaitingLobbyPanel()
    {
        WaitingLobbyPanel.SetActive(true);
        quizPanel.SetActive(false); // Deactivate the quiz panel
    }

    void EndGame()
    {
        finalScorePanel.SetActive(true);
        quizPanel.SetActive(false); // Deactivate the quiz panel
        WaitingLobbyPanel.SetActive(false); 
        StopSound(finalSecondsAudioSource); // Stop any ongoing sounds

        // Update topic scores in the final score panel
        UpdateTopicScores();
    }

    void UpdateTopicScores()
    {
        for (int i = 0; i < topics.Length; i++)
        {
            string topic = topics[i];
            player1TopicScoreTexts[i].text = GetTopicScoreText(player1TopicScores[topic]);
            player2TopicScoreTexts[i].text = GetTopicScoreText(player2TopicScores[topic]);
        }
    }

    string GetTopicScoreText(int score)
    {
        if (score == 0) return "-";
        return new string('+', score);
    }

    void ResetGame()
    {
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnReset);
    }

    void ExitToResetScene()
    {
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnReset);
    }

    public void ExitToDisconnectScene()
    {
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnDisconnect);
    }

    private void RaiseScoreEvent()
    {
        object[] data = { player1Score, player2Score };
        PhotonNetwork.RaiseEvent(UpdateScoreEventCode, data, RaiseEventOptions.Default, SendOptions.SendReliable);
    }

    private void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == 3) // Timer update event
        {
            float receivedTimer = (float)photonEvent.CustomData;

            // Ensure the timer is synchronized for the second player
            if (!PhotonNetwork.IsMasterClient && Mathf.Abs(timer - receivedTimer) > 0.05f)
            {
                timer = receivedTimer;
            }

            UpdateTimerUI();
        }
        else if (photonEvent.Code == 1)
        {
            isPlayer1Turn = (bool)photonEvent.CustomData;
            UpdatePanels();
        }
        else if (photonEvent.Code == UpdateScoreEventCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            player1Score = (int)data[0];
            player2Score = (int)data[1];
            UpdateScores();
        }
    }

    private void UpdateTimerForAllPlayers(float newTimer)
    {
        PhotonNetwork.RaiseEvent(3, newTimer, RaiseEventOptions.Default, SendOptions.SendReliable);
    }

    void UpdatePanels()
    {
        bool isCurrentPlayer = PhotonNetwork.IsMasterClient == isPlayer1Turn;

        answeringPanel.SetActive(isCurrentPlayer);
        waitingPanel.SetActive(!isCurrentPlayer);

        player1Shadow.SetActive(!isPlayer1Turn);
        player2Shadow.SetActive(isPlayer1Turn);

        foreach (Button btn in answerButtons)
        {
            btn.interactable = isCurrentPlayer;
        }

        skipButton.interactable = isCurrentPlayer;
    }

    void UpdateProcessImage(string processName)
    {
        employeeOnboardingImage.gameObject.SetActive(processName == "Employee Onboarding");
        conflictResolutionImage.gameObject.SetActive(processName == "Conflict Resolution");
        trainingAndDevelopmentImage.gameObject.SetActive(processName == "Training and Development");
        workplaceDiversityAndInclusionImage.gameObject.SetActive(processName == "Workplace Diversity and Inclusion");
        employeeEngagementImage.gameObject.SetActive(processName == "Employee Engagement");
        teamCollaborationImage.gameObject.SetActive(processName == "Team collaboration");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        playerLeftGameObject.SetActive(true);
        StartCoroutine(HandlePlayerLeft());
    }

    private IEnumerator HandlePlayerLeft()
    {
        yield return new WaitForSeconds(7f);
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnDisconnect);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isPlayer1Turn);
            stream.SendNext(player1Score);
            stream.SendNext(player2Score);
        }
        else
        {
            isPlayer1Turn = (bool)stream.ReceiveNext();
            player1Score = (int)stream.ReceiveNext();
            player2Score = (int)stream.ReceiveNext();
        }
    }

    void UpdatePlayerLanguages()
    {
        // Get the language for each player from Photon Player Custom Properties
        string player1Language = LanguageManager.GetPlayerLanguage(PhotonNetwork.PlayerList[0].ActorNumber);
        string player2Language = LanguageManager.GetPlayerLanguage(PhotonNetwork.PlayerList[1].ActorNumber);

        // Update the language processes for each player
        if (PhotonNetwork.IsMasterClient)
        {
            selectedLanguage = player1Language;
        }
        else
        {
            selectedLanguage = player2Language;
        }

        Debug.Log($"Player 1 Language: {player1Language}, Player 2 Language: {player2Language}");
    }
}

[System.Serializable]
public class LanguageProcesses
{
    public string language; // Language code (e.g., "EN", "SW", "IT", "GR", "RO")
    public Process[] processes; // Processes for the language
}

[System.Serializable]
public class Process
{
    public string name; // Process name
    public Question[] questions; // Array of questions in the process
    private int currentQuestionIndex = 0;

    public Question GetNextQuestion()
    {
        if (currentQuestionIndex < questions.Length)
        {
            return questions[currentQuestionIndex++];
        }

        return null; // No more questions
    }
}

[System.Serializable]
public class Question
{
    public string text; // Question text
    public string[] answers; // Array of answers
    public int correctAnswer; // Index of the correct answer
    public string outcome; // Outcome text

    // Topics as boolean fields
    public bool motivationAndLeadership;
    public bool empathyAndRelationshipBuilding;
    public bool communicationSkills;
    public bool selfAwarenessAndSelfReflection;
    public bool adaptabilityAndFlexibility;
    public bool conflictManagementAndProblemSolving;
    public bool culturalIntelligenceAndDiversity;
    public bool emotionalRegulationAndResilience;
    public bool teamworkAndCollaboration;
    public bool criticalThinking;
    public bool ethicalDecisionMaking;
    public bool accountabilityAndResponsibility;
    public bool inclusionAndTeamBuilding;
    public bool planningAndOrganization;
    public bool strategicProblemSolving;
    public bool conflictResolution;
    public bool timeManagement;
    public bool collaborationAndLeadership;
    public bool changeManagement;
    public bool engagementStrategies;
    public bool collaboration;
    public bool emotionalRegulation;
    public bool empathy;

    public string GetSelectedTopic()
    {
        if (motivationAndLeadership) return "Motivation & Leadership";
        if (empathyAndRelationshipBuilding) return "Empathy & Relationship-Building";
        if (communicationSkills) return "Communication Skills";
        if (selfAwarenessAndSelfReflection) return "Self-Awareness & Self-Reflection";
        if (adaptabilityAndFlexibility) return "Adaptability & Flexibility";
        if (conflictManagementAndProblemSolving) return "Conflict Management & Problem-Solving";
        if (culturalIntelligenceAndDiversity) return "Cultural Intelligence & Diversity";
        if (emotionalRegulationAndResilience) return "Emotional Regulation & Resilience";
        if (teamworkAndCollaboration) return "Teamwork & Collaboration";
        if (criticalThinking) return "Critical Thinking";
        if (ethicalDecisionMaking) return "Ethical decision-making";
        if (accountabilityAndResponsibility) return "Accountability & Responsibility";
        if (inclusionAndTeamBuilding) return "Inclusion & Team Building";
        if (planningAndOrganization) return "Planning & Organization";
        if (strategicProblemSolving) return "Strategic Problem-Solving";
        if (conflictResolution) return "Conflict Resolution";
        if (timeManagement) return "Time Management";
        if (collaborationAndLeadership) return "Collaboration & Leadership";
        if (changeManagement) return "Change Management";
        if (engagementStrategies) return "Engagement Strategies";
        if (collaboration) return "Collaboration";
        if (emotionalRegulation) return "Emotional Regulation";
        if (empathy) return "Empathy";
        return "Unknown";
    }
}