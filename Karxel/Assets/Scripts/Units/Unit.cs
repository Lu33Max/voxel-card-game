using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UnitMarkerManager))]
public class Unit : NetworkBehaviour
{
    private enum LiveStatus : byte
    {
        Alive,
        Dead,
    }
    
    public enum StatusEffect : byte
    {
        Shielded,
        Stunned,
        PreStunned,
    }
    
    [Serializable]
    private class UnitMaterial
    {
        public Material white;
        public Material black;
        public Material unavailable;
    }
    
    /// <summary> Used to attach status effects to a unit for a given amount of turns </summary>
    public class UnitStatus
    {
        /// <summary> Type of status effect to attach to the unit </summary>
        public StatusEffect Status;
        /// <summary> Number of turns this effect stays active for. Negative numbers indicate infinite duration </summary>
        public int Duration;
    }
    
    [field: SyncVar, SerializeField] public Team owningTeam { get; private set; } = Team.None;
    [HideInInspector, SyncVar(hook = nameof(OnControlStatusChanged))] public bool isControlled;
    
    [Header("UnitData")]
    [SerializeField] protected UnitData data = null!;
    
    [Header("Unit Material")] 
    [SerializeField] private MeshRenderer meshRenderer = null!;
    [SerializeField] private UnitMaterial unitMaterial = null!;
    [SerializeField] protected Material outlineMaterial = null!;
    
    [Header("Health Visualization")]
    [SerializeField] private Slider healthSlider = null!;
    [SerializeField] private TextMeshProUGUI healthCounter = null!;

    /// <summary> ONLY INVOKED ON SERVER </summary>
    public static event Action<UnitBehaviour, Team>? OnUnitDied;
    
    [field: SyncVar] public Vector3Int TilePosition { get; private set; }
    public UnitData Data => data;

    [SyncVar(hook = nameof(OnHealthUpdated))] private int _currentHealth;
    [SyncVar] private LiveStatus _status = LiveStatus.Alive;

    private readonly List<MoveCommand> _moveIntents = new();
    private readonly List<Attack> _attackIntents = new();
    private readonly SyncList<UnitStatus> _statusEffects = new();

    /// <summary> Boolean to track whether damage was taken in the current round to remove the shield status </summary>
    private bool _tookDamage;
    
    private AudioSource _sfxSource = null!;
    private PathManager _pathManager = null!;
    private UnitEffectDisplay _effectDisplay = null!;
    
    /// <summary> Contains all unit-specific functionalities like movement and attacking </summary>
    private UnitBehaviour _behaviour = null!;
    /// <summary> Plays animations for movement and attacks </summary>
    private UnitAnimator _animator = null!;

    public UnitMarkerManager UnitMarkerManager { get; private set; } = null!;

    private MoveCommand? _tempRegisteredMove;
    private Attack? _tempRegisteredAttack;
    
    private void Awake()
    {
        UnitMarkerManager = GetComponent<UnitMarkerManager>();
        
        _sfxSource = GetComponent<AudioSource>();
        _pathManager = GetComponent<PathManager>();
        _behaviour = GetComponent<UnitBehaviour>();
        _animator = GetComponent<UnitAnimator>();
        _effectDisplay = GetComponentInChildren<UnitEffectDisplay>();
        
        healthSlider.GetComponent<HealthSlider>().SetupSliderColor(owningTeam);
        _pathManager.Setup(this);
    }

    private void Start()
    {
        GameManager.Instance!.GameStateChanged += OnGameStateChanged;
        HandManager.OnCardSelected += UpdateMaterialForCurrentCard;
        HandManager.OnCardPlayed += UpdateMaterialToTeamColor;
        HandManager.OnCardDeselected += UpdateMaterialToTeamColor;
        
        _statusEffects.OnAdd += AddEffectToDisplay;
        _statusEffects.OnRemove += RemoveEffectToDisplay;
        
        UpdateMaterialToTeamColor();

        if(!isServer) return;
        
        UpdateHealth(data.health);
        UnitActionManager.Instance!.OnTileDamaged += OnAttackExecuted;
        GameManager.Instance.CheckHealth += OnCheckHealth;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateMaterialToTeamColor();
        healthSlider.GetComponent<HealthSlider>().SetupSliderColor(owningTeam);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        
        if(GameManager.Instance) GameManager.Instance.GameStateChanged -= OnGameStateChanged;
        
        HandManager.OnCardSelected -= UpdateMaterialForCurrentCard;
        HandManager.OnCardPlayed -= UpdateMaterialToTeamColor;
        HandManager.OnCardDeselected -= UpdateMaterialToTeamColor;
        
        _statusEffects.OnAdd -= AddEffectToDisplay;
        _statusEffects.OnRemove -= RemoveEffectToDisplay;
        
        if(!isServer)
            return;
        
        if(UnitActionManager.Instance) UnitActionManager.Instance.OnTileDamaged -= OnAttackExecuted;
        if(GameManager.Instance) GameManager.Instance.CheckHealth -= OnCheckHealth;
    }

    /// <summary>Get all tiles currently reachable by the unit. Only includes valid moves.</summary>
    /// <param name="movementRange">The movement range given by the played card</param>
    /// <param name="positionOverride"> Optional override if the calculation should take place from a different tile </param>
    public IEnumerable<MoveCommand> GetValidMoves(int movementRange, Vector3Int? positionOverride = null)
    {
        return _behaviour.GetValidMoves(positionOverride ?? PositionAfterMove, movementRange);
    }

    /// <summary> Calculates a list of unique tile positions that can be attacked from the current unit position </summary>
    /// <param name="positionOverride"> Optional override if the calculation should take place from a different tile </param>
    public List<Vector3Int> GetValidAttackTiles(Vector3Int? positionOverride = null) =>
        _behaviour.GetValidAttackTiles(positionOverride ?? TilePosition);

    /// <summary>
    ///     Calculates which tiles should be attacked based on the currently hovered tile. Returns nul if no valid tile
    ///     is hovered
    /// </summary>
    /// <param name="hoveredPos"> Position of the currently hovered tile </param>
    /// <param name="damageMultiplier"> DamageMultiplier from the currently active card </param>
    public Attack? GetAttackForHoverPosition(Vector3Int hoveredPos, int damageMultiplier) =>
        _behaviour.GetAttackForHoverPosition(hoveredPos, damageMultiplier);

    /// <summary> Returns whether the unit can currently be selected by a new player </summary>
    public bool IsSelectable => _status == LiveStatus.Alive && !IsStunned && ActionAmountLeft > 0 && !isControlled;

    /// <summary> Returns true whenever the amount of registered move intents this turn is at least 1 </summary>
    public bool HasMoveIntentsRegistered => _moveIntents.Count > 0;

    /// <summary> Returns the amount of moves this unit is still allowed to do this turn </summary>
    public int ActionAmountLeft => GameManager.Instance.gameState switch
    {
        GameState.Movement => Data.moveAmount - _moveIntents.Count,
        GameState.Attack => Data.attackAmount - _attackIntents.Count,
        _ => 0
    };

    /// <summary> Returns true when the current health equals the maximum health </summary>
    public bool HasMaxHealth => _currentHealth == Data.health;

    /// <summary> Returns whether the unit is stunned during the current attack or movement round </summary>
    private bool IsStunned => _statusEffects.FindIndex(s => s.Status == StatusEffect.Stunned) >= 0;
    
    /// <summary> Returns the tile position the unit would have after executing all MoveIntents </summary>
    private Vector3Int PositionAfterMove => _moveIntents.Count > 0 ? _moveIntents.Last().TargetPosition : TilePosition;

    /// <summary> Checks whether the given type is currently active on the unit </summary>
    /// <param name="effect"> Type of StatusEffect to search for </param>
    /// <param name="duration"> Optional parameter on the duration of turns left </param>
    public bool HasEffectOfTypeActive(StatusEffect effect, int duration = 0)
    {
        return _statusEffects
                    .FirstOrDefault(s => s.Status == effect && (duration == 0 || s.Duration == duration)) != null;
    }
    
    /// <summary> Plays the move animation and reports to the <see cref="GameManager"/> once done </summary>
    /// <param name="moveCommand"> Move chain to execute </param>
    private IEnumerator MoveToPositions(MoveCommand moveCommand)
    {
        foreach (var worldPos in moveCommand.Path
                                             .Select(tile => GridManager.Instance.GridToWorldPosition(tile))
                                             .Where(worldPos => worldPos != null))
            yield return StartCoroutine(_animator.PlayMoveAnimation(worldPos!.Value));
        
        var targetPos = GridManager.Instance.GridToWorldPosition(moveCommand.TargetPosition);
        
        if(targetPos.HasValue)
            yield return StartCoroutine(_animator.PlayMoveAnimation(targetPos.Value));

        if (moveCommand.BlockedPosition.HasValue)
        {
            var blockedPos = GridManager.Instance.GridToWorldPosition(moveCommand.BlockedPosition.Value);
            
            if(blockedPos.HasValue)
                yield return StartCoroutine(_animator.PlayBlockedAnimation(blockedPos.Value));
        }
        
        UnitActionManager.Instance.CmdUnitActionDone();
    }

    /// <summary> Plays the attack animation and reports to the <see cref="GameManager"/> once done </summary>
    /// <param name="attack"> The attack to execute </param>
    private IEnumerator ExecuteAttack(Attack attack)
    {
       yield return StartCoroutine(_animator.PlayAttackAnimation(attack));
       UnitActionManager.Instance.CmdUnitActionDone();
    }

    private void OnHealthUpdated(int _, int newHealth)
    {
        healthSlider.value = (float)newHealth / data.health;
        healthCounter.text = newHealth.ToString();
    }

    private void OnControlStatusChanged(bool _, bool isNowSelected)
    {
        // Only display selection highlight for other team members
        if(owningTeam != Player.LocalPlayer.team)
            return;
        
        var newMaterials = isNowSelected
            ? meshRenderer.materials.Append(outlineMaterial).ToArray()
            : new[] { meshRenderer.materials.First() };
        
        meshRenderer.materials = newMaterials;
    }

    protected virtual void OnGameStateChanged(GameState newState)
    {
        _moveIntents.Clear();
        _attackIntents.Clear();
        
        if(isServer && newState is GameState.Attack or GameState.Movement)
            CheckForStatusDurations();
    }
    
    private void UpdateMaterialToTeamColor()
    {
        meshRenderer.material = owningTeam == Team.Blue ? unitMaterial.white : unitMaterial.black;
    }

    /// <summary> Grays out a unit whenever a new card is selected that cannot be played on this unit </summary>
    /// <param name="selectedCard"> The card currently selected by the player </param>
    private void UpdateMaterialForCurrentCard(CardData selectedCard)
    {
        // A card can be played on a unit if it...
        // a) is a disposable card and the card says that it's valid to be used on this unit or
        // b) is a move or attack card, the unit can be selected and is of the player's own team
        if ((selectedCard.IsDisposable() &&
            selectedCard.CanBeUsed(GridManager.Instance.GetTileAtGridPosition(TilePosition), null)) ||
            (!selectedCard.IsDisposable() && IsSelectable && owningTeam == Player.LocalPlayer.team))
        {
            UpdateMaterialToTeamColor();
            return;
        }

        meshRenderer.material = unitMaterial.unavailable;
    }

    /// <summary> Updates the duration of all status effects and removes expired ones </summary>
    [Server]
    private void CheckForStatusDurations()
    {
        var addStun = false;
        
        for (var i = _statusEffects.Count - 1; i >= 0; i--)
        {
            var newStatus = new UnitStatus{ Status = _statusEffects[i].Status, Duration = _statusEffects[i].Duration - 1};
            _statusEffects[i] = newStatus;
                
            if (_statusEffects[i].Duration != 0) continue;

            if (_statusEffects[i].Status == StatusEffect.PreStunned)
                addStun = true;
            
            _statusEffects.RemoveAt(i);
        }
        
        if(addStun) _statusEffects.Add(new UnitStatus{ Status = StatusEffect.Stunned, Duration = 1 });

        if (!_tookDamage) return;
        
        _tookDamage = false;

        var shieldIndex = _statusEffects.FindIndex(s => s.Status == StatusEffect.Shielded);
        if(shieldIndex >= 0) _statusEffects.RemoveAt(shieldIndex);
    }

    [Server]
    private void OnAttackExecuted(Vector3Int position, UnitActionManager.DamageHealCount damage)
    {
        if(position != TilePosition)
            return;

        var totalDamage = damage.Heal;

        if (HasEffectOfTypeActive(StatusEffect.Shielded))
        {
            totalDamage += Mathf.RoundToInt(damage.Damage / 2f);
            _tookDamage = true;
        }
        else totalDamage += damage.Damage;
        
        UpdateHealth(-totalDamage);
    }
    
    [Server]
    private void UpdateHealth(int changeAmount)
    {
        _currentHealth = Mathf.Clamp(_currentHealth + changeAmount, 0, data.health);

        if (changeAmount > 0) return;
        
        PlayHurtSound();
        if(_currentHealth <= 0) StartCoroutine(Die());
    }

    [Server]
    private void OnCheckHealth()
    {
        if (_currentHealth > 0) 
            return;
        
        //Logging
        ActionLogger.Instance.LogAction("server", owningTeam.ToString(), "died", null, 
            null, gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());
        
        StartCoroutine(Die());
    }
    
    [Command(requiresAuthority = false)]
    public void CmdUpdateHealth(int changeAmount)
    {
        UpdateHealth(changeAmount);
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateControlStatus(bool newState)
    {
        isControlled = newState;
    }

    [Server]
    public void UpdateGridPosition(Vector3Int startingPosition)
    {
        TilePosition = startingPosition;
    }
    
    [ClientRpc]
    public void RPCStep(MoveCommand moveCommand)
    {
        StartCoroutine(MoveToPositions(moveCommand));
    }

    [ClientRpc]
    public void RPCExecuteAttack(Attack attack)
    {
        StartCoroutine(ExecuteAttack(attack));
    }

    [ClientRpc]
    private void PlayHurtSound()
    {
        AudioManager.PlaySfx(_sfxSource, AudioManager.Instance.UnitHurt);
    }

    [ClientRpc]
    protected virtual void RpcDie()
    {
        GridManager.Instance.RemoveUnit(TilePosition);
        Destroy(gameObject);
    }

    [Server]
    private IEnumerator Die()
    {
        _status = LiveStatus.Dead;
        OnUnitDied?.Invoke(_behaviour, owningTeam);
        
        // Play death animation coroutine and sfx
        // yield return StartCoroutine(...);

        yield return new WaitForSeconds(1);
        
        RpcDie();
    }

    /// <summary> Adds the given UnitStatus to the list of active StatusEffects </summary>
    [Command(requiresAuthority = false)]
    public void CmdAddNewStatusEffect(UnitStatus newEffect)
    {
        _statusEffects.Add(newEffect);
    }

    [Server]
    public void ServerAddNewStatusEffect(UnitStatus newEffect)
    {
        _statusEffects.Add(newEffect);
    }
    
    private void AddEffectToDisplay(int addedIndex)
    {
        _effectDisplay.AddEffect(_statusEffects[addedIndex].Status);
    }
    
    private void RemoveEffectToDisplay(int _, UnitStatus removedStatus)
    {
        if(_statusEffects.Count(e => e.Status == removedStatus.Status) == 0)
            _effectDisplay.RemoveEffect(removedStatus.Status);
    }

    /// <summary> Used to locally store a potential move until it's been validated by the server </summary>
    /// <param name="command"> The command to execute locally </param>
    /// <param name="moveCard"> The card that was played on this move </param>
    [Client]
    public void ExecuteMoveLocally(MoveCommand command, MoveCardData moveCard)
    {
        _tempRegisteredMove = command;

        _pathManager.CreatePathLocally(command,
            _moveIntents.Count > 0 ? _moveIntents.Last().TargetPosition : TilePosition);
        
        UnitActionManager.Instance.CmdTryRegisterMoveIntent(Player.LocalPlayer.netId, Player.LocalPlayer.team, 
            TilePosition, command, moveCard.moveDistance);
    }

    /// <summary> Called on the client that tried to register, confirming registration was successful </summary>
    /// <param name="target"> Targeted NetworkConnection </param>
    /// <param name="command"> The validated <see cref="MoveCommand"/> </param>
    [TargetRpc]
    public void TargetOnMoveRegisterSuccessful(NetworkConnectionToClient target, MoveCommand command)
    {
        _moveIntents.Add(command);
        _tempRegisteredMove = null;
    }

    /// <summary> Called on all team clients except the registering one upon successful move validation </summary>
    /// <param name="target"> Targeted NetworkConnection </param>
    /// <param name="command"> The validated <see cref="MoveCommand"/> </param>
    [TargetRpc]
    public void TargetRegisterNewMoveIntent(NetworkConnectionToClient target, MoveCommand command)
    {
        _moveIntents.Add(command);
        _pathManager.CreatePathLocally(command,
            _moveIntents.Count > 0 ? _moveIntents.Last().TargetPosition : TilePosition);
    }

    /// <summary>
    ///     Called in case the validation of the new move has failed. Removes local displays and re-adds played card
    /// </summary>
    /// <param name="target"> Targeted NetworkConnection </param>
    [TargetRpc]
    public void TargetUndoLocallyRegisteredMove(NetworkConnectionToClient target)
    {
        if(_tempRegisteredMove == null) return;
        
        _pathManager.RegeneratePathLocally(_moveIntents);
        HandManager.Instance.RestoreLastPlayedCard();
        
        _tempRegisteredMove = null;
    }

    /// <summary> Used to locally store a potential attack until it's been validated by the server </summary>
    /// <param name="attack"> The attack to execute locally </param>
    /// <param name="attackCard"> The card that was played on this attack </param>
    [Client]
    public void ExecuteAttackLocally(Attack attack, AttackCardData attackCard)
    {
        _tempRegisteredAttack = attack;

        foreach (var tile in attack.Tiles)
            MarkerManager.Instance.AddMarkerLocal(tile, new MarkerData
            {
                Type = MarkerType.Attack,
                Priority = 1,
                Visibility = "local"
            });
        
        UnitActionManager.Instance.CmdTryRegisterAttackIntent(Player.LocalPlayer.netId, Player.LocalPlayer.team, 
            TilePosition, attack, attackCard.damageMultiplier);
    }
    
    /// <summary> Called on the client that tried to register, confirming registration was successful </summary>
    /// <param name="target"> Targeted NetworkConnection </param>
    /// <param name="attack"> The validated <see cref="Attack"/> </param>
    [TargetRpc]
    public void TargetOnAttackRegisterSuccessful(NetworkConnectionToClient target, Attack attack)
    {
        _attackIntents.Add(attack);
        if(_tempRegisteredAttack == null) return;
        
        foreach (var tile in _tempRegisteredAttack.Tiles)
            MarkerManager.Instance.RemoveMarkerLocal(tile, MarkerType.Attack, "local");
        
        _tempRegisteredAttack = null;
    }
    
    /// <summary> Called on all team clients except the registering one upon successful attack validation </summary>
    /// <param name="target"> Targeted NetworkConnection </param>
    /// <param name="attack"> The validated <see cref="Attack"/> </param>
    [TargetRpc]
    public void TargetRegisterNewAttackIntent(NetworkConnectionToClient target, Attack attack)
    {
        _attackIntents.Add(attack);
    }

    /// <summary>
    ///     Called in case the validation of the new attack has failed. Removes local displays and re-adds played card
    /// </summary>
    /// <param name="target"> Targeted NetworkConnection </param>
    [TargetRpc]
    public void TargetUndoLocallyRegisteredAttack(NetworkConnectionToClient target)
    {
        if(_tempRegisteredAttack == null) return;
        
        foreach (var tile in _tempRegisteredAttack.Tiles)
            MarkerManager.Instance.RemoveMarkerLocal(tile, MarkerType.Attack, "local");
        
        HandManager.Instance.RestoreLastPlayedCard();
        _tempRegisteredAttack = null;
    }
}
