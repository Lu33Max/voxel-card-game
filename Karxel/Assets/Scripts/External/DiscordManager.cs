using System;
using UnityEngine;
using Discord;

public class DiscordManager : MonoBehaviour
{
    public enum ActivityState {
        Menu,
        Lobby,
        Game
    }
    
    public static DiscordManager Instance;
    
    private Discord.Discord _discord;
    private Activity _activity;
    private ActivityState _lastState;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _discord = new Discord.Discord(1342122312463417396, (ulong)CreateFlags.NoRequireDiscord);
        _activity = new Activity();
        
        UpdateActivity(ActivityState.Menu);
    }

    private void OnDisable()
    {
        _discord.Dispose();
    }
    
    public void UpdateActivity(ActivityState state, Team team = Team.None, int playerCount = 0, int round = 0)
    {
        var activityManager = _discord.GetActivityManager();

        if (_lastState != state || _activity.Timestamps.Start == 0)
        {
            _activity.Timestamps = new ActivityTimestamps
            {
                Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            _lastState = state;
        }

        _activity.State = state switch
        {
            ActivityState.Menu => "In Main Menu",
            ActivityState.Lobby => "In Lobby",
            ActivityState.Game => "Playing online",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        _activity.Details = state switch
        {
            ActivityState.Menu => "",
            ActivityState.Lobby => "Waiting for start",
            ActivityState.Game => $"Playing {team.ToString()} - Round {round}",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        _activity.Assets = new ActivityAssets
        {
            LargeImage = state != ActivityState.Game ? "default" :
                team == Team.Blue ? "online_playing_blue" : "online_playing_red",
            /*LargeText*/
        };

        if (state != ActivityState.Menu)
        {
            _activity.Party = new ActivityParty
            {
                Id = "ae488379-351d-4a4f-ad32-2b9b01c91657",
                Size = { CurrentSize = playerCount, MaxSize = 8 }
            };

            _activity.Secrets = new ActivitySecrets
            {
                Join = "MTI4NzM0OjFpMmhuZToxMjMxMjM"
            };
        }
        
        activityManager.UpdateActivity(_activity, res =>
        {
            if(res != Result.Ok)
                Debug.LogWarning(res.ToString());
        });
    }

    private void Update()
    {
        _discord.RunCallbacks();
    }
}
