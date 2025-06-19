using System.Collections.Generic;
using UnityEngine;
using I2.Loc;
using Photon.Pun;
using Photon.Realtime; // Add this namespace
using ExitGames.Client.Photon;

public class LanguageManager : MonoBehaviour
{
    public static Dictionary<int, string> PlayerLanguages { get; private set; } = new Dictionary<int, string>(); // Per-player language settings
    public static event System.Action<int, string> OnPlayerLanguageChanged; // Event for per-player language change

    private const byte LanguageUpdateEventCode = 4; // Custom Photon event code for language updates

    // Map full language names to their codes
    private readonly Dictionary<string, string> languageMap = new Dictionary<string, string>
    {
        { "English", "EN" },
        { "Swedish", "SW" },
        { "Italian", "IT" },
        { "Greek", "EL" },
        { "Romanian", "RO" },
        { "French", "FR" },
        { "French (France)", "FR-FR" },
        { "Turkish", "TR" },
        { "Dutch", "NL" },
        { "Dutch (Netherlands)", "NL-NL" }
    };

    void Awake()
    {
        DontDestroyOnLoad(gameObject); // Ensure the LanguageManager persists across scenes
    }

    void Start()
    {
        // Initialize the default language for the local player
        int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!PlayerLanguages.ContainsKey(localPlayerId))
        {
            PlayerLanguages[localPlayerId] = "EN"; // Default to English
        }

        // Ensure the correct language is set on start
        ChangeLanguage(localPlayerId, LocalizationManager.CurrentLanguage);
    }

    /// <summary>
    /// Changes the language for a specific player and refreshes the UI.
    /// </summary>
    /// <param name="playerId">The ID of the player whose language is being changed.</param>
    /// <param name="languageName">The name of the language to switch to.</param>
    public void ChangeLanguage(int playerId, string languageName)
    {
        // Attempt to map the full language name to its code
        if (languageMap.TryGetValue(languageName, out string languageCode))
        {
            PlayerLanguages[playerId] = languageCode; // Store the selected language code locally

            if (playerId == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                LocalizationManager.CurrentLanguage = languageName; // Update the localization system for the local player
                Debug.Log($"Language successfully changed for Player {playerId} to: {languageName} ({languageCode})");

                OnPlayerLanguageChanged?.Invoke(playerId, languageCode); // Notify listeners about the language change

                // Store the language in Photon Player Custom Properties
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "Language", languageCode } });

                // Force a refresh on all localized components
                LocalizationManager.LocalizeAll(true);
            }
        }
        else
        {
            Debug.LogError($"Language '{languageName}' is not available. Please add it to the language map.");
        }
    }

    private void BroadcastLanguageChange(int playerId, string languageCode)
    {
        if (PhotonNetwork.InRoom)
        {
            object[] data = { playerId, languageCode };
            PhotonNetwork.RaiseEvent(LanguageUpdateEventCode, data, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
        }
        else
        {
            Debug.LogWarning("Player is not in a Photon room. Language change will be stored locally and synchronized later.");
        }
    }

    void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        PhotonNetwork.NetworkingClient.StateChanged += OnPhotonStateChanged; // Listen for state changes
    }

    void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        PhotonNetwork.NetworkingClient.StateChanged -= OnPhotonStateChanged; // Remove state change listener
    }

    private void OnPhotonStateChanged(Photon.Realtime.ClientState fromState, Photon.Realtime.ClientState toState)
    {
        if (toState == Photon.Realtime.ClientState.Joined)
        {
            // Synchronize the local player's language when they join a room
            int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
            if (PlayerLanguages.TryGetValue(localPlayerId, out string languageCode))
            {
                BroadcastLanguageChange(localPlayerId, languageCode);
            }
        }
    }

    private void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == LanguageUpdateEventCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            int playerId = (int)data[0];
            string languageCode = (string)data[1];

            PlayerLanguages[playerId] = languageCode; // Update the language for the player
            Debug.Log($"Received language update: Player {playerId} -> {languageCode}");
        }
    }

    /// <summary>
    /// Gets the language code for a specific player.
    /// </summary>
    /// <param name="playerId">The ID of the player.</param>
    /// <returns>The language code for the player, or "EN" if not set.</returns>
    public static string GetPlayerLanguage(int playerId)
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            // Retrieve the language from Photon Player Custom Properties
            Photon.Realtime.Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerId);
            if (player != null && player.CustomProperties.TryGetValue("Language", out object languageCode))
            {
                return languageCode as string;
            }
        }

        // Fallback to the local dictionary if not in a room
        if (PlayerLanguages.TryGetValue(playerId, out string localLanguageCode))
        {
            return localLanguageCode;
        }

        return "EN"; // Default to English if not set
    }

    // Add a method to get the current language for the local player
    public static string GetLocalPlayerLanguage()
    {
        int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
        return GetPlayerLanguage(localPlayerId);
    }

    /// <summary>
    /// Called by buttons to change the language.
    /// </summary>
    /// <param name="languageName">The name of the language to switch to.</param>
    public void ChangeLanguage(string languageName)
    {
        int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
        ChangeLanguage(localPlayerId, languageName);
    }
}
