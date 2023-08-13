using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TwitchAPI;
using TwitchChat;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class CounterTwitchGame : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameTMP;
    [SerializeField] private TextMeshProUGUI currentScoreTMP;
    [SerializeField] private TextMeshProUGUI maxScoreTMP;

    private int currentScore;

    private string lastUsername = string.Empty;

    private int currentMaxScore;
    private readonly string maxScoreKey = "maxScore";

    private string currentMaxScoreUsername = "RothioTome";
    private readonly string maxScoreUsernameKey = "maxScoreUsername";

    private string lastUserIdVIPGranted;
    private readonly string lastUserIdVIPGrantedKey = "lastVIPGranted";

    private string nextPotentialVIP;

    [SerializeField] private GameObject startingCanvas;


    Dictionary<char, int> romanValues = new Dictionary<char, int>
    {
        {'I', 1},
        {'V', 5},
        {'X', 10},
        {'L', 50},
        {'C', 100},
        {'D', 500},
        {'M', 1000}
    };


    private void Start()
    {
        Application.targetFrameRate = 30;

        TwitchController.onTwitchMessageReceived += OnTwitchMessageReceived;
        TwitchController.onChannelJoined += OnChannelJoined;

        currentMaxScore = PlayerPrefs.GetInt(maxScoreKey);
        currentMaxScoreUsername = PlayerPrefs.GetString(maxScoreUsernameKey, currentMaxScoreUsername);
        lastUserIdVIPGranted = PlayerPrefs.GetString(lastUserIdVIPGrantedKey, string.Empty);

        UpdateMaxScoreUI();
        UpdateCurrentScoreUI(lastUsername, currentScore.ToString());
        ResetGame();
    }

    private void OnDestroy()
    {
        TwitchController.onTwitchMessageReceived -= OnTwitchMessageReceived;
        TwitchController.onChannelJoined -= OnChannelJoined;
    }

    private void OnTwitchMessageReceived(Chatter chatter)
    {
        string romanNumeral = chatter.message.ToUpper(); // Convertir a mayúsculas para manejo uniforme

        string displayName = chatter.IsDisplayNameFontSafe() ? chatter.tags.displayName : chatter.login;

        if (lastUsername.Equals(displayName)) return;

        if (!UseRomanLetters(romanNumeral)) return;

        if (!IsValidRomanNumeral(romanNumeral)) {

            HandleIncorrectResponse(displayName, chatter);
            
        }else
        { 
            int numeralValue = ConvertRomanToInteger(romanNumeral);
        

            if (numeralValue == currentScore + 1)
            {
                HandleCorrectResponse(displayName, chatter, romanNumeral);
            }
            else
            {
                HandleIncorrectResponse(displayName, chatter);
            }
        }
    }

    private bool UseRomanLetters(string romanNumeral)
    {
        string pattern = "^[IVXLCDM]+$";

        return Regex.IsMatch(romanNumeral, pattern);
    }

    private bool IsValidRomanNumeral(string numeral)
    {
        string pattern = "^M{0,3}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$"; // Expresión regular para validar los caracteres romanos        
        
        return Regex.IsMatch(numeral, pattern);
    }


    
    private int ConvertRomanToInteger(string numeral)
    {
        int result = 0;
        int prevValue = 0;

        for (int i = numeral.Length - 1; i >= 0; i--)
        {
            int currentValue = romanValues[numeral[i]];

            if (currentValue < prevValue)
            {
                result -= currentValue;
            }
            else
            {
                result += currentValue;
            }

            prevValue = currentValue;
        }

        return result;
    }

    private void HandleCorrectResponse(string displayName, Chatter chatter, string romanNumeral)
    {
        currentScore++;
        UpdateCurrentScoreUI(displayName, romanNumeral);

        lastUsername = displayName;
        if (currentScore > currentMaxScore)
        {
            SetMaxScore(displayName, currentScore);
            HandleVIPStatusUpdate(chatter);
        }
    }

    private void HandleIncorrectResponse(string displayName, Chatter chatter)
    {
        if (currentScore != 0)
        {
            DisplayShameMessage(displayName);

            if (TwitchOAuth.Instance.IsVipEnabled())
            {
                if (lastUserIdVIPGranted.Equals(chatter.tags.userId))
                {
                    RemoveLastVIP();
                }

                HandleNextPotentialVIP();
            }

            HandleTimeout(chatter);
            UpdateMaxScoreUI();
            ResetGame();
        }
    }

    private void HandleNextPotentialVIP()
    {
        if (!string.IsNullOrEmpty(nextPotentialVIP))
        {
            if (nextPotentialVIP == "-1")
            {
                RemoveLastVIP();
            }
            else
            {
                if (!string.IsNullOrEmpty(lastUserIdVIPGranted))
                {
                    RemoveLastVIP();
                }

                GrantVIPToNextPotentialVIP();
            }

            nextPotentialVIP = string.Empty;
        }
    }

    private void HandleTimeout(Chatter chatter)
    {
        if (TwitchOAuth.Instance.IsModImmunityEnabled())
        {
            if (!chatter.HasBadge("moderator"))
            {
                TwitchOAuth.Instance.Timeout(chatter.tags.userId, currentScore);
            }
        }
        else
        {
            TwitchOAuth.Instance.Timeout(chatter.tags.userId, currentScore);
        }
    }

    private void HandleVIPStatusUpdate(Chatter chatter)
    {
        if (TwitchOAuth.Instance.IsVipEnabled())
        {
            if (!chatter.tags.HasBadge("vip"))
            {
                nextPotentialVIP = chatter.tags.userId;
            }
            else if (chatter.tags.userId == lastUserIdVIPGranted)
            {
                nextPotentialVIP = "";
            }
            else
            {
                nextPotentialVIP = "-1";
            }
        }
    }

    private void RemoveLastVIP()
    {
        TwitchOAuth.Instance.SetVIP(lastUserIdVIPGranted, false);
        lastUserIdVIPGranted = "";
        PlayerPrefs.SetString(lastUserIdVIPGrantedKey, lastUserIdVIPGranted);
    }

    private void GrantVIPToNextPotentialVIP()
    {
        TwitchOAuth.Instance.SetVIP(nextPotentialVIP, true);
        lastUserIdVIPGranted = nextPotentialVIP;
        PlayerPrefs.SetString(lastUserIdVIPGrantedKey, lastUserIdVIPGranted);
    }

    private void DisplayShameMessage(string displayName)
    {
        usernameTMP.SetText($"<color=#00EAC0>Shame on </color>{displayName}<color=#00EAC0>!</color>");
    }

    private void OnChannelJoined()
    {
        startingCanvas.SetActive(false);
    }

    public void ResetHighScore()
    {
        SetMaxScore("RothioTome", 0);
        RemoveLastVIP();
        ResetGame();
    }

    private void SetMaxScore(string username, int score)
    {
        currentMaxScore = score;
        currentMaxScoreUsername = username;
        PlayerPrefs.SetString(maxScoreUsernameKey, username);
        PlayerPrefs.SetInt(maxScoreKey, score);
        UpdateMaxScoreUI();
    }

    private void UpdateMaxScoreUI()
    {
        string scoreText = $"HIGH SCORE: {currentMaxScore}\nby <color=#00EAC0>";

        if (TwitchOAuth.Instance.IsVipEnabled() &&
            (!string.IsNullOrEmpty(nextPotentialVIP) || !string.IsNullOrEmpty(lastUserIdVIPGranted)))
        {
            scoreText += $"<sprite=0>{currentMaxScoreUsername}</color>";
        }
        else
        {
            scoreText += currentMaxScoreUsername;
        }

        maxScoreTMP.SetText(scoreText);
    }

    private void UpdateCurrentScoreUI(string username, string score)
    {
        usernameTMP.SetText(username);
        currentScoreTMP.SetText(score);
    }

    private void ResetGame()
    {
        lastUsername = "";
        currentScore = 0;
        currentScoreTMP.SetText(currentScore.ToString());
    }

}