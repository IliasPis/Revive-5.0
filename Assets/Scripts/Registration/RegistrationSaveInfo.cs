using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Newtonsoft.Json;

public class RegistrationSaveInfo : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Dropdown countryDropdown;
    [SerializeField] private List<string> countryOptions;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in anonymously. Player ID: {AuthenticationService.Instance.PlayerId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to sign in: {e.Message}");
            }
        }

        PopulateCountryDropdown();
    }

    private void PopulateCountryDropdown()
    {
        if (countryDropdown != null && countryOptions != null)
        {
            countryDropdown.ClearOptions();
            countryDropdown.AddOptions(countryOptions);
        }
        else
        {
            Debug.LogWarning("Country dropdown or options list is not assigned.");
        }
    }

    public async void SaveRegistrationInfo()
    {
        string name = nameInputField.text;
        string country = countryDropdown.options[countryDropdown.value].text;

        // Use the player's unique ID as part of the key prefix
        string playerId = AuthenticationService.Instance.PlayerId;

        try
        {
            // Save each piece of data under its own key (email removed)
            await CloudSaveService.Instance.Data.ForceSaveAsync(new Dictionary<string, object>
            {
                { $"{playerId}_Name", name },
                { $"{playerId}_Country", country }
            });

            Debug.Log($"Player information saved to Cloud Save: Name = {name}, Country = {country}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save player information: {e.Message}");
        }
    }

    public async void RetrievePlayerDetails()
    {
        try
        {
            // Use the player's unique ID as part of the key prefix
            string playerId = AuthenticationService.Instance.PlayerId;

            // Retrieve each piece of data using its own key (email removed)
            var savedData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string>
            {
                $"{playerId}_Name",
                $"{playerId}_Country"
            });

            // Check and log each piece of data (email removed)
            string name = savedData.TryGetValue($"{playerId}_Name", out string retrievedName) ? retrievedName : "N/A";
            string country = savedData.TryGetValue($"{playerId}_Country", out string retrievedCountry) ? retrievedCountry : "N/A";

            Debug.Log($"Retrieved player data: Name = {name}, Country = {country}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to retrieve player information: {e.Message}");
        }
    }

    public void DisplayPlayerInfo()
    {
        // Display the player's unique ID from Player Management
        string playerId = AuthenticationService.Instance.PlayerId;
        Debug.Log($"Player ID (from Player Management): {playerId}");

        // Retrieve and display custom data from Cloud Save
        RetrievePlayerDetails();
    }
}

