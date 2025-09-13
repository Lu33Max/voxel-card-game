using System;
using UnityEngine;
using Discord;

public class DiscordManager : Singleton<DiscordManager>
{
    public enum ActivityState {
        Menu,
        Lobby,
        Game
    }
    
    private Discord.Discord? _discord;
    private Activity _activity;
    private ActivityState _lastState;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        SetupDiscord();
        _activity = new Activity();
        UpdateActivity(ActivityState.Menu);
    }
    
    private void Update()
    {
        _discord?.RunCallbacks();
    }

    private void OnDestroy()
    {
        _discord?.Dispose();
    }
    
    public void UpdateActivity(ActivityState state, Team team = Team.None, int playerCount = 0, int round = 0)
    {
        // If no client has been set up, retry in case the user has opened the app in the meantime
        if (_discord == null)
        {
            SetupDiscord();
            
            if(_discord == null) return;
        };
        
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

    private void SetupDiscord()
    {
        try
        {
            _discord = new Discord.Discord(1342122312463417396, (ulong)CreateFlags.NoRequireDiscord);
        }
        catch (Exception _)
        {
            _discord = null;
        }
    }
}
