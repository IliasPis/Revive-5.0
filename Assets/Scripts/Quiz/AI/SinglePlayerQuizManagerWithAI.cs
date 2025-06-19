using UnityEngine;
using TMPro; // For TextMeshPro
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun; // For PhotonNetwork

public class SinglePlayerQuizManagerWithAI : MonoBehaviour
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
    public Image computerAvatar; // Avatar for the computer
    public TextMeshProUGUI player1NameText; // Name Text for Player 1
    public TextMeshProUGUI player2NameText; // Name Text for the computer
    public GameObject finalScorePanel; // Final score panel
    public GameObject quizPanel; // Quiz panel to deactivate
    public GameObject WaitingLobbyPanel;
    public GameObject playerLeftGameObject; // GameObject to show when a player leaves
    public GameObject outcomePanel; // Outcome panel
    public TextMeshProUGUI outcomeText; // Outcome Text
    public GameObject quizTaken; // Quiz taken object

    [Header("Quiz Data")]
    public LanguageProcessess[] languageProcesses; // Processes organized by language
    private int currentQuestionIndex = 0;
    private int questionsAnsweredInTurn = 0;
    private Dictionary<string, int> processQuestionTracker;

    [Header("Game Settings")]
    public int playerQuestions = 20; // Number of questions for the player
    public int computerQuestions = 20; // Number of questions for the computer
    public int questionsPerProcess = 2; // Minimum questions per process per game
    public float timerDuration = 30f;
    public string selectedLanguage = "EN"; // Default language
    public int questionsBeforeSwitch = 2; // Questions before switching turn (adjustable in Editor)
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

    private int totalQuestionsToAnswer; // Total questions to be answered by both player and computer

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

    private Coroutine turnCheckCoroutine;

    private const string AvatarKey = "Avatar"; // Custom Property key for avatars

    public SinglePlayerFinalScore finalScoreScript; // Reference to the SinglePlayerFinalScore script

    // Add public getter methods
    public int GetPlayer1Score()
    {
        return player1Score;
    }

    public int GetPlayer2Score()
    {
        return player2Score;
    }

    public float GetTotalTime()
    {
        return totalTime;
    }

    // Store original button colors
    private Dictionary<Button, Color> originalButtonColors = new Dictionary<Button, Color>();

    void Start()
{
    isPlayer1Turn = true;

    // Initialize process tracker
    InitializeProcessTracker();

    // Initialize topic scores
    InitializeTopicScores();

    // Initialize UI and Timer
    timer = timerDuration;
    InitializeAvatarsAndNames();
    InitializePlayerNames(); // Initialize player names

    selectedLanguage = LanguageManager.GetLocalPlayerLanguage(); // Get the selected language for the local player
    Debug.Log($"Selected Language for Single Player: {selectedLanguage}");

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

    // Calculate total questions to answer
    totalQuestionsToAnswer = playerQuestions + computerQuestions;
    Debug.Log($"Total Questions to Answer: {totalQuestionsToAnswer}");
}

    void Update()
    {
        if (!finalScorePanel.activeSelf) // If the game is not over
        {
            totalTime += Time.deltaTime; // Update total time played

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

            // Continuously check the total questions answered
            if (totalQuestionsAnswered >= totalQuestionsToAnswer)
            {
                EndGame();
            }
        }

        // Extra counter to ensure continuous update
        if (totalQuestionsAnswered < totalQuestionsToAnswer)
        {
            Debug.Log($"Checking total questions answered: {totalQuestionsAnswered}/{totalQuestionsToAnswer}");
        }
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
    isAnswering = false; // Reset answering state
    finalSecondsPlaying = false; // Reset final seconds sound state
    StopSound(finalSecondsAudioSource); // Stop any looping sounds

    selectedLanguage = LanguageManager.GetLocalPlayerLanguage(); // Update the language dynamically
    LanguageProcessess languageProcess = System.Array.Find(languageProcesses, lang => lang.language == selectedLanguage);
    if (languageProcess == null)
    {
        Debug.LogError($"No data available for language: {selectedLanguage}");
        return;
    }

    List<Questionn> availableQuestions = new List<Questionn>();

    foreach (var process in languageProcess.processes)
    {
        foreach (var question in process.questions)
        {
            question.processName = process.name; // Ensure processName is set
            if (processQuestionTracker[process.name] < questionsPerProcess || totalQuestionsAnswered < totalQuestionsToAnswer)
            {
                availableQuestions.Add(question);
            }
        }
    }

    if (availableQuestions.Count == 0)
    {
        Debug.Log("All questions have been answered.");
        EndGame();
        return;
    }

    Questionn selectedQuestion = availableQuestions[Random.Range(0, availableQuestions.Count)];
    processQuestionTracker[selectedQuestion.processName]++;
    questionText.text = selectedQuestion.text;

    for (int i = 0; i < answerButtons.Length; i++)
    {
        if (i < selectedQuestion.answers.Length)
        {
            answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = selectedQuestion.answers[i];
            answerButtons[i].onClick.RemoveAllListeners();
            int answerIndex = i;
            answerButtons[i].onClick.AddListener(() => CheckAnswer(answerIndex, selectedQuestion.correctAnswer, selectedQuestion.GetSelectedTopicc(), selectedQuestion.outcome));
            answerButtons[i].gameObject.SetActive(true);
        }
        else
        {
            answerButtons[i].gameObject.SetActive(false);
        }
    }

    timer = timerDuration;
    messageText.text = "";
    UpdateTimerUI();

    if (!isPlayer1Turn)
    {
        float delay = Random.Range(5f, 10f);
        Invoke(nameof(ComputerAnswerQuestion), delay);
    }

    currentQuestionIndex++;
    Debug.Log($"Loaded Question: {selectedQuestion.text}");
}

    void ComputerAnswerQuestion()
    {
        int chosenIndex;
        if (Random.value > 0.5f) // 50% chance to answer the question correctly
        {
            chosenIndex = System.Array.FindIndex(answerButtons, btn => btn.GetComponentInChildren<TextMeshProUGUI>().text == questionText.text);
        }
        else // 50% chance to answer the question incorrectly
        {
            chosenIndex = Random.Range(0, answerButtons.Length);
        }

        // Ensure the chosen index is within bounds
        if (chosenIndex < 0 || chosenIndex >= answerButtons.Length)
        {
            chosenIndex = 0; // Default to the first button if out of bounds
        }

        // Get the correct topic for the question
        string topic = GetTopicFromQuestion();

        CheckAnswer(chosenIndex, answerButtons[chosenIndex].GetComponentInChildren<TextMeshProUGUI>().text == questionText.text ? 1 : 0, topic, "Random Outcome");
    }

    string GetTopicFromQuestion()
    {
        // Logic to determine the topic from the current question
        // This should match the logic used in the Questionn class
        // For simplicity, let's assume the topic is always "Unknown" for now
        return "Unknown";
    }

  public void CheckAnswer(int chosenIndex, int correctIndex, string topic, string outcome)
{
    if (isAnswering) return;

    isAnswering = true;

    Button selectedButton = answerButtons[chosenIndex];

    if (chosenIndex == correctIndex)
    {
        messageText.text = "Correct Answer!";
        HighlightButton(selectedButton, Green); // Use standard green
        if (isPlayer1Turn)
        {
            player1Score++;
            if (player1TopicScores.ContainsKey(topic))
            {
                player1TopicScores[topic]++;
            }
        }
        else
        {
            player2Score++;
            if (player2TopicScores.ContainsKey(topic))
            {
                player2TopicScores[topic]++;
            }
        }
        UpdateScores();
    }
    else
    {
        messageText.text = "Wrong Answer!";
        HighlightButton(selectedButton, Red); // Use standard red
        HighlightButton(answerButtons[correctIndex], Green); // Use standard green
    }

    questionsAnsweredInTurn++;
    totalQuestionsAnswered++; // Increment total questions answered
    Debug.Log($"Total Questions Answered: {totalQuestionsAnswered}");

    // Wait 3 seconds before showing the outcome and proceeding
    StartCoroutine(ShowOutcomeAfterDelay(outcome));
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
        SwitchTurn();
    }
    else
    {
        LoadQuestion();
    }
}

// Use standard green and red for correct and wrong answers
static readonly Color Green = Color.green;
static readonly Color Red = Color.red;
static readonly Color DefaultButtonColor = Color.white; // Or set to your default image color

void HighlightButton(Button button, Color color)
{
    var img = button.GetComponent<Image>();
    if (img != null)
    {
        // Store original color if not already stored
        if (!originalButtonColors.ContainsKey(button))
            originalButtonColors[button] = img.color;
        img.color = color;
    }
}

// Reset all button images to their original color
void ResetButtonColors()
{
    foreach (Button btn in answerButtons)
    {
        var img = btn.GetComponent<Image>();
        if (img != null && originalButtonColors.ContainsKey(btn))
            img.color = originalButtonColors[btn];
    }
}

void OnTimeOver()
{
    messageText.text = "Time Over! Question Passed.";
    StopSound(finalSecondsAudioSource); // Stop any looping sounds
    questionsAnsweredInTurn++;
    totalQuestionsAnswered++; // Increment total questions answered
    Debug.Log($"Total Questions Answered: {totalQuestionsAnswered}");

    // Check if the current player has finished answering all their questions
    if (totalQuestionsAnswered >= totalQuestionsToAnswer) // Ensure both player and AI finish
    {
        EndGame();
    }
    else
    {
        // Check if the player needs to switch turns
        if (questionsAnsweredInTurn >= questionsBeforeSwitch)
        {
            questionsAnsweredInTurn = 0;
            SwitchTurn();
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
    totalQuestionsAnswered++; // Increment total questions answered
    Debug.Log($"Total Questions Answered: {totalQuestionsAnswered}");

    // Check if the current player has finished answering all their questions
    if (totalQuestionsAnswered >= totalQuestionsToAnswer) // Ensure both player and AI finish
    {
        EndGame();
    }
    else
    {
        // Check if the player needs to switch turns
        if (questionsAnsweredInTurn >= questionsBeforeSwitch)
        {
            questionsAnsweredInTurn = 0;
            SwitchTurn();
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
    }

    void InitializePlayerNames()
    {
        player1NameText.text = PhotonNetwork.NickName;
        player2NameText.text = "Computer";
    }

    void InitializeAvatarsAndNames()
    {
        // Assign Player 1 avatar from Photon custom properties
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(AvatarKey, out object avatar))
        {
            string avatarName = avatar as string;
            player1Avatar.sprite = Resources.Load<Sprite>($"Avatars/{avatarName}"); // Ensure sprites are stored in a Resources/Avatars folder
            finalScoreScript.SetPlayerAvatar(player1Avatar.sprite); // Provide avatar sprite to final score script
        }

        // Assign static computer avatar from content
        computerAvatar.sprite = Resources.Load<Sprite>("Avatars/ComputerAvatar"); // Ensure to assign the computer avatar in the Unity editor
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

    void ShowFinalScorePanel()
    {
        finalScorePanel.SetActive(true);
        quizPanel.SetActive(false); // Deactivate the quiz panel
        waitingPanel.SetActive(false); // Deactivate the waiting panel
        StopSound(finalSecondsAudioSource); // Stop any ongoing sounds

        // Update topic scores in the final score panel
        UpdateTopicScores();

        // Update final scores and time played
        UpdateFinalScores();
    }

    void ShowWaitingLobbyPanel()
    {
        WaitingLobbyPanel.SetActive(true);
        quizPanel.SetActive(false); // Deactivate the quiz panel when showing waiting lobby
    }

void EndGame()
{
    Debug.Log("EndGame called.");
    finalScorePanel.SetActive(true);
    quizPanel.SetActive(false); // Deactivate the quiz panel
    waitingPanel.SetActive(false); // Deactivate the waiting panel
    StopSound(finalSecondsAudioSource); // Stop any ongoing sounds

    // Provide scores, total time, and player name to the final score script
    finalScoreScript.SetScores(player1Score, player2Score);
    finalScoreScript.SetTotalTime(totalTime);
    finalScoreScript.SetPlayerName(PhotonNetwork.NickName); // Provide player name to final score script
    finalScoreScript.SetPlayerAvatar(player1Avatar.sprite); // Provide player avatar to final score script

    // Update topic scores in the final score panel
    UpdateTopicScores();

    // Update final scores and time played
    UpdateFinalScores();
}

void UpdateTopicScores()
{
    for (int i = 0; i < topics.Length; i++)
    {
        string topic = topics[i];
        if (i < player1TopicScoreTexts.Length)
        {
            player1TopicScoreTexts[i].text = GetPlayer1TopicScoreText(player1TopicScores[topic]);
        }
    }
}

string GetPlayer1TopicScoreText(int score)
{
    if (score == 0) return "-";
    return new string('+', score);
}

string GetPlayer2TopicScoreText(int score)
{
    if (score == 0) return "-";
    return new string('+', score);
}

void UpdateFinalScores()
{
    // Update the final scores and time played
    totalTimeText.text = $"Total Time: {totalTimeText.text}";
    if (player1Score > player2Score)
    {
        player1ScoreText.text = $"Winner: {player1Score}";
        player2ScoreText.text = $"Loser: {player2Score}";
    }
    else if (player2Score > player1Score)
    {
        player1ScoreText.text = $"Loser: {player1Score}";
        player2ScoreText.text = $"Winner: {player2Score}";
    }
    else
    {
        player1ScoreText.text = $"Draw: {player1Score}";
        player2ScoreText.text = $"Draw: {player2Score}";
    }

    // Update topic scores in the final score panel
    UpdateTopicScores();
}

    void ResetGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnReset);
    }

    void ExitToResetScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnReset);
    }

    public void ExitToDisconnectScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnDisconnect);
    }

    void UpdatePanels()
    {
        bool isCurrentPlayer = isPlayer1Turn;

        answeringPanel.SetActive(true);
        waitingPanel.SetActive(true);

        if (isCurrentPlayer)
        {
            answeringPanel.transform.SetAsLastSibling();
        }
        else
        {
            waitingPanel.transform.SetAsLastSibling();
        }

        player1Shadow.SetActive(!isPlayer1Turn);
        player2Shadow.SetActive(isPlayer1Turn);

        foreach (Button btn in answerButtons)
        {
            btn.interactable = isCurrentPlayer;
        }

        skipButton.interactable = isCurrentPlayer;
    }
    
    void SwitchTurn()
    {
        isPlayer1Turn = !isPlayer1Turn; // Toggle the turn
        PlaySound(turnChangeAudioSource); // Play turn change sound
        UpdatePanels(); // Update UI panels
        LoadQuestion(); // Load the next question
    }
}

[System.Serializable]
public class LanguageProcessess
{
    public string language; // Language code (e.g., "EN", "SW", "IT", "GR", "RO")
    public Processs[] processes; // Processes for the language
}

[System.Serializable]
public class Processs
{
    public string name; // Process name
    public Questionn[] questions; // Array of questions in the process
    private int currentQuestionIndex = 0;

    public Questionn GetNextQuestionn()
    {
        if (currentQuestionIndex < questions.Length)
        {
            return questions[currentQuestionIndex++];
        }

        return null; // No more questions
    }
}

[System.Serializable]
public class Questionn
{
    public string text; // Question text
    public string[] answers; // Array of answers
    public int correctAnswer; // Index of the correct answer
    public string outcome; // Outcome text
    public string processName; // Process name

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

    public string GetSelectedTopicc()
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