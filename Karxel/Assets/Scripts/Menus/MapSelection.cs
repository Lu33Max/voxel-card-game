using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MapSelection : NetworkBehaviour
{
    [SerializeField] private Transform mapIconParent;
    [SerializeField] private GameObject voteIcon;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private int timerDuration = 10;

    [SerializeField] private AudioClip timerRegular;
    
    private List<Toggle> _toggles = new();
    private List<Transform> _voteGroups = new();
    private List<int> _playerVotes = new();

    private float _timeLeft;
    private bool _selected;
    private int _selectedCount;
    
    private void Start()
    {
        for (var i = 0; i < mapIconParent.childCount; i++)
        {
            var index = i;
            var toggle = mapIconParent.GetChild(i).GetComponent<Toggle>();
            var group = toggle.GetComponentInChildren<HorizontalLayoutGroup>().transform;
            
            toggle.onValueChanged.AddListener(delegate { OnTogglePressed(index);});
            
            _toggles.Add(toggle);
            _voteGroups.Add(group);
            _playerVotes.Add(0);
        }
    }

    private void Update()
    {
        if(_timeLeft <= 0)
            return;

        _timeLeft -= Time.deltaTime;
        
        if(_timeLeft <= 0 && isServer)
            CmdSelectMap();

        var newTime = Mathf.FloorToInt(_timeLeft + 1).ToString();

        if(newTime != timerText.text && _timeLeft < 3)
            AudioManager.Instance.PlaySFX(timerRegular);
        
        timerText.text = newTime;
    }

    public void StartTimer()
    {
        _timeLeft = timerDuration;
    }

    private void OnTogglePressed(int toggleIndex)
    {
        if (_toggles[toggleIndex].isOn)
            CmdUpdateVotes(toggleIndex, 1, _selected);
        else
            CmdUpdateVotes(toggleIndex, -1, _selected);
        
        _selected = true;
    }

    [Command(requiresAuthority = false)]
    private void CmdSelectMap()
    {
        List<int> potentialMaps = new();

        var currentMax = 0;
        
        for(int i = 0; i < _playerVotes.Count; i++)
        {
            if (_playerVotes[i] == currentMax)
            {
                potentialMaps.Add(i);
            }
            else if(_playerVotes[i] > currentMax)
            {
                currentMax = _playerVotes[i];
                potentialMaps.Clear();
                potentialMaps.Add(i);
            }
        }
        
        RpcSelectedMap(potentialMaps[Random.Range(0, potentialMaps.Count)]);
    }
    

    [Command(requiresAuthority = false)]
    private void CmdUpdateVotes(int toggleIndex, int change, bool hadAlreadySelected)
    {
        RpcUpdateVotes(toggleIndex, change);
        
        if(hadAlreadySelected)
            return;
        
        _selectedCount++;
        if (_selectedCount == NetworkServer.connections.Count)
            RpcSkipTimer();
    }
    
    [ClientRpc]
    private void RpcUpdateVotes(int toggleIndex, int change)
    {
        _playerVotes[toggleIndex] += change;

        var voteDiff = _playerVotes[toggleIndex] - _voteGroups[toggleIndex].childCount;

        if (voteDiff < 0)
            for (int i = 0; i < Mathf.Abs(voteDiff); i++)
                Destroy(_voteGroups[toggleIndex].GetChild(i).gameObject);
        else if (voteDiff > 0)
            for (int i = 0; i < voteDiff; i++)
                Instantiate(voteIcon, _voteGroups[toggleIndex]);
    }

    [ClientRpc]
    private void RpcSkipTimer()
    {
        if(_timeLeft > 2f)
            _timeLeft = 1.99f;
    }

    [ClientRpc]
    private void RpcSelectedMap(int mapIndex)
    {
        SceneData.MapIndex = mapIndex;
        
        if(isServer)
            FindObjectOfType<LobbyManager>().StartGame();
    }
}
