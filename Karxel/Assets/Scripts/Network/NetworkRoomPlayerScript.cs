using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Custom Room Player for Lobby
/// Mirror overrides for custom functionality
/// Joining and Leaving Lobby and Ready State
/// </summary>
public class NetworkRoomPlayerScript : NetworkRoomPlayer
{
    [SyncVar(hook = nameof(OnTeamChanged))]
    public Team team;

    [SyncVar(hook = nameof(OnReadyStatusChanged))]
    public bool isReady;

    public void SetTeam(Team newTeam)
    {
        if (isReady)
        {
            Debug.LogWarning("Du kannst das Team nicht ändern, während du bereit bist.");
            return;
        }

        CmdSetTeam(newTeam);
    }

    [Command]
    private void CmdSetTeam(Team newTeam)
    {
        team = newTeam;
    }

    private void OnTeamChanged(Team oldTeam, Team newTeam)
    {
        Debug.Log($"Team geändert: {oldTeam} -> {newTeam}");
        // UI aktualisieren
    }

    public void SetReady(bool readyStatus)
    {
        CmdChangeReadyState(readyStatus);
    }

    private void OnReadyStatusChanged(bool oldStatus, bool newStatus)
    {
        Debug.Log($"Ready Status geändert: {oldStatus} -> {newStatus}");
        // UI aktualisieren
    }
    
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        base.ReadyStateChanged(oldReadyState, newReadyState);
        isReady = newReadyState; // Synchronisiere unsere eigene Ready-Variable
    }
}