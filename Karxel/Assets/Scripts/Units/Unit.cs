using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UnitMarkerManager))]
public abstract class Unit : NetworkBehaviour
{
    private enum LiveStatus
    {
        Alive,
        Dead,
    }
    
    public enum StatusEffect
    {
        Shielded,
        Stunned,
    }
    
    /// <summary> Used to attach status effects to a unit for a given amount of turns </summary>
    public class UnitStatus
    {
        /// <summary> Type of status effect to attach to the unit </summary>
        public StatusEffect Status;
        
        /// <summary> Number of turns this effect stays active for. Negative numbers indicate infinite duration </summary>
        public int Duration;
    }
    
    [SyncVar] public Team owningTeam = Team.None;
    [HideInInspector, SyncVar(hook = nameof(OnControlStatusChanged))] public bool isControlled;
    
    [Header("UnitData")]
    [SerializeField] protected UnitData data;
    [SerializeField] private int moveLimit = 3;
    [SerializeField] private int attackLimit = 1;
    
    [Header("Movement")] 
    [SerializeField] private float moveArcHeight = 0.3f;
    
    [Header("Visualization")]
    [SerializeField] private GameObject canvas;
    [SerializeField] protected Material outlineMaterial;
    
    [Header("Health Visualization")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthCounter;

    [Header("Unit Action Display")]
    [SerializeField] private Transform actionDisplayParent;
    [SerializeField] private GameObject actionImagePrefab;
    
    public Vector3Int TilePosition { get; private set; }
    protected List<MoveCommand> MoveIntent { get; } = new();
    private List<Attack> AttackIntent { get; } = new();
    public UnitData Data => data;

    [SyncVar(hook = nameof(OnHealthUpdated))] private int _currentHealth;
    [SyncVar] private LiveStatus _status = LiveStatus.Alive;
    
    private readonly SyncList<UnitStatus> _statusEffects = new();

    /// <summary> Boolean to track whether damage was taken in the current round to remove the shield status </summary>
    private bool _tookDamage;
    
    private Transform _camera;
    private MeshRenderer _renderer;
    private AudioSource _sfxSource;
    private PathManager _pathManager;
    private UnitEffectDisplay _effectDisplay;
    
    public UnitMarkerManager MarkerManager { get; private set; }

    /// <summary>Get all tiles currently reachable by the unit. Only includes valid moves.</summary>
    /// <param name="movementRange">The movement range given by the played card</param>
    public abstract IEnumerable<MoveCommand> GetValidMoves(int movementRange);

    public abstract List<Vector3Int> GetValidAttackTiles(Vector3Int? positionOverride = null);
    
    [CanBeNull]
    public abstract Attack GetAttackForHoverPosition(Vector3Int hoveredPos, int damageMultiplier);

    private void Start()
    {
        if (Camera.main != null) 
            _camera = Camera.main.transform;
        _renderer = GetComponentInChildren<MeshRenderer>();
        _sfxSource = GetComponent<AudioSource>();
        _pathManager = GetComponent<PathManager>();
        _pathManager.Setup(this);

        _effectDisplay = GetComponentInChildren<UnitEffectDisplay>();

        MarkerManager = GetComponent<UnitMarkerManager>();
        
        GameManager.Instance.GameStateChanged += OnGameStateChanged;
        
        healthSlider.GetComponent<HealthSlider>().SetupSliderColor(owningTeam);

        if(!isServer)
            return;
        
        UpdateHealth(data.health);
        GameManager.Instance.AttackExecuted += OnAttackExecuted;
        GameManager.Instance.CheckHealth += OnCheckHealth;
    }

    private void Update()
    {
        canvas.transform.LookAt(_camera);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        GameManager.Instance.GameStateChanged -= OnGameStateChanged;
        
        if(!isServer)
            return;
        
        GameManager.Instance.AttackExecuted -= OnAttackExecuted;
        GameManager.Instance.CheckHealth -= OnCheckHealth;
    }

    /// <summary> Instantly move the unit to the given tile </summary>
    public void MoveToTile(Vector3Int tilePos)
    {
        var worldPos = GridManager.Instance.GridToWorldPosition(tilePos);
        
        if(worldPos != null)
            CmdChangePosition(worldPos.Value, tilePos);
    }

    public bool CanBeSelected()
    {
        var gameState = GameManager.Instance.gameState;

        return _status == LiveStatus.Alive && ((gameState == GameState.Attack && AttackIntent.Count < attackLimit) ||
                                               (gameState == GameState.Movement && MoveIntent.Count < moveLimit)) &&
               !IsStunned;
    }

    public bool HasMoveIntentsRegistered()
    {
        return MoveIntent.Count > 0;
    }

    /// <summary> Returns whether the unit is stunned during the current attack or movement round </summary>
    private bool IsStunned => _statusEffects.Find(s => s.Status == StatusEffect.Stunned && s.Duration == 1) != null;

    /// <summary> Checks whether the given type is currently active on the unit </summary>
    /// <param name="effect"> Type of StatusEffect to search for </param>
    /// <param name="duration"> Optional parameter on the duration of turns left </param>
    public bool HasEffectOfTypeActive(StatusEffect effect, int duration = 0)
    {
        return _statusEffects.Find(s => s.Status == effect && (duration == 0 || s.Duration == duration)) != null;
    }
    
    // Move the unit along the given path from tile to tile
    private IEnumerator MoveToPositions(MoveCommand moveCommand)
    {
        foreach (var worldPos in moveCommand.Path
                                             .Select(tile => GridManager.Instance.GridToWorldPosition(tile))
                                             .Where(worldPos => worldPos.HasValue))
        {
            yield return StartCoroutine(Move(worldPos.Value));
        }
        
        var targetPos = GridManager.Instance.GridToWorldPosition(moveCommand.TargetPosition);
        
        if(targetPos.HasValue)
            yield return StartCoroutine(Move(targetPos.Value));

        if (moveCommand.BlockedPosition.HasValue)
        {
            var blockedPos = GridManager.Instance.GridToWorldPosition(moveCommand.BlockedPosition.Value);
            
            if(blockedPos.HasValue)
                yield return StartCoroutine(BlockedAnimation(blockedPos.Value));
        }
        
        GameManager.Instance.CmdUnitMovementDone();
    }

    // Move the unit to the given world position
    private IEnumerator Move(Vector3 targetPos)
    {
        var startingPos = transform.position;
        var direction = targetPos - startingPos;
        direction.y = 0;
        
        var targetRotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : transform.rotation;
        AudioManager.PlaySfx(_sfxSource, AudioManager.Instance.UnitMove);
        
        float elapsedTime = 0;
        var unitTransform = transform;
        
        while (elapsedTime < data.stepDuration)
        {
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                unitTransform.rotation = Quaternion.Lerp(unitTransform.rotation, targetRotation, 15 * Time.deltaTime);
            
            var progression = elapsedTime / data.stepDuration;
            var newPos = Vector3.Lerp(startingPos, targetPos, progression);
            
            // Parabolic movement
            var baseHeight = Mathf.Lerp(startingPos.y, targetPos.y, progression);
            var height = 4 * moveArcHeight * progression * (1 - progression);
            newPos.y = baseHeight + height;

            unitTransform.position = newPos;
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        
        unitTransform.rotation = targetRotation;
        unitTransform.position = targetPos;
    }

    private IEnumerator BlockedAnimation(Vector3 targetPos)
    {
        var startingPos = transform.position;
        var direction = targetPos - startingPos;
        direction.y = 0;
        
        var targetRotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : transform.rotation;
        AudioManager.PlaySfx(_sfxSource, AudioManager.Instance.UnitMove);
        
        float elapsedTime = 0;
        var unitTransform = transform;
        
        while (elapsedTime < data.stepDuration / 2)
        {
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                unitTransform.rotation = Quaternion.Lerp(unitTransform.rotation, targetRotation, 15 * Time.deltaTime);
            
            var progression = elapsedTime / data.stepDuration;
            var newPos = Vector3.Lerp(startingPos, targetPos, progression);
            
            // Parabolic movement
            var baseHeight = Mathf.Lerp(startingPos.y, targetPos.y, progression);
            var height = 4 * moveArcHeight * progression * (1 - progression);
            newPos.y = baseHeight + height;

            unitTransform.position = newPos;
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        
        while (elapsedTime < data.stepDuration)
        {
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                unitTransform.rotation = Quaternion.Lerp(unitTransform.rotation, targetRotation, 15 * Time.deltaTime);
            
            var progression = elapsedTime / data.stepDuration;
            var newPos = Vector3.Lerp(targetPos, startingPos, progression);
            
            // Parabolic movement
            var baseHeight = Mathf.Lerp(targetPos.y, startingPos.y, progression);
            var height = 4 * moveArcHeight * progression * (1 - progression);
            newPos.y = baseHeight + height;

            unitTransform.position = newPos;
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        unitTransform.rotation = targetRotation;
        unitTransform.position = startingPos;
    }

    /// <summary>Used to play animations, sfx etc.</summary>
    private IEnumerator Attack(Attack attack)
    {
        var startingPos = transform.position;
        var direction = GridManager.Instance.GridToWorldPosition(attack.Tiles[0])!.Value - startingPos;
        direction.y = 0;
        
        var targetRotation = Quaternion.LookRotation(direction);
        
        var unitTransform = transform;
        var elapsedTime = 0f;

        while (elapsedTime < data.stepDuration)
        {
            if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                unitTransform.rotation = Quaternion.Lerp(unitTransform.rotation, targetRotation, 15 * Time.deltaTime);
            
            var newPos = unitTransform.position;
            var progression = elapsedTime / data.stepDuration;
            newPos.y = startingPos.y + 4 * moveArcHeight * progression * (1 - progression);
            
            unitTransform.position = newPos;
            
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        unitTransform.position = startingPos;
        unitTransform.rotation = targetRotation;
        
        GameManager.Instance.CmdUnitAttackDone();
    }
    
    protected static List<Vector3Int> ReconstructPath(IReadOnlyDictionary<Vector3Int, Vector3Int> path, Vector3Int endPosition, Vector3Int startPosition)
    {
        List<Vector3Int> constructedPath = new();
        var currentPosition = endPosition;
        
        while (path.TryGetValue(currentPosition, out var prevPosition) && prevPosition != startPosition)
        {
            constructedPath.Add(prevPosition);
            currentPosition = prevPosition;
        }

        // The reconstruction works from end to beginning, so it has to be reversed
        constructedPath.Reverse();

        return constructedPath;
    }

    private void OnHealthUpdated(int old, int newHealth)
    {
        healthSlider.value = (float)newHealth / data.health;
        healthCounter.text = newHealth.ToString();
    }

    private void OnControlStatusChanged(bool old, bool isNowSelected)
    {
        // Only display selection highlight for other team members
        if(owningTeam != GameManager.Instance.localPlayer.team)
            return;
        
        var newMaterials = isNowSelected
            ? _renderer.materials.Append(outlineMaterial).ToArray()
            : new[] { _renderer.materials.First() };
        
        _renderer.materials = newMaterials;
    }

    private void UpdateActionDisplayAfterAction()
    {
        if(owningTeam != GameManager.Instance.localPlayer.team)
            return;
        
        var displayCount = actionDisplayParent.childCount;
        
        if(GameManager.Instance.gameState == GameState.Movement)
            for(var i = 0; i < displayCount - (moveLimit - MoveIntent.Count); i++)
                Destroy(actionDisplayParent.GetChild(i).gameObject);
        
        else if(GameManager.Instance.gameState == GameState.Attack)
            for(var i = 0; i < displayCount - (attackLimit - AttackIntent.Count); i++)
                Destroy(actionDisplayParent.GetChild(i).gameObject);
    }

    private void ClearActionDisplay()
    {
        var childCount = actionDisplayParent.childCount;
        
        for(var i = 0; i < childCount; i++)
            Destroy(actionDisplayParent.GetChild(i).gameObject);
    }

    private void UpdateActionDisplayOnStateUpdate(GameState newState)
    {
        if(GameManager.Instance == null || GameManager.Instance.localPlayer == null ||
           owningTeam != GameManager.Instance.localPlayer.team)
            return;
        
        ClearActionDisplay();

        if(newState == GameState.Movement)
            for(var i = 0; i < moveLimit; i++)
            {
                var newDisplay = Instantiate(actionImagePrefab, actionDisplayParent);
                newDisplay.GetComponent<ActionDisplayImage>().SetIcon(ActionDisplayType.Move);
            }
        else if(newState == GameState.Attack)
            for(var i = 0; i < attackLimit; i++)
            {
                var newDisplay = Instantiate(actionImagePrefab, actionDisplayParent);
                newDisplay.GetComponent<ActionDisplayImage>().SetIcon(ActionDisplayType.Attack);
            }
    }

    protected virtual void OnGameStateChanged(GameState newState)
    {
        if(isServer && newState is GameState.Attack or GameState.Movement)
            CheckForStatusDurations();
        
        UpdateActionDisplayOnStateUpdate(newState);
    }

    /// <summary> Updates the duration of all status effects and removes expired ones </summary>
    [Server]
    private void CheckForStatusDurations()
    {
        for (var i = _statusEffects.Count - 1; i >= 0; i--)
        {
            var newStatus = new UnitStatus{ Status = _statusEffects[i].Status, Duration = _statusEffects[i].Duration - 1};
            _statusEffects[i] = newStatus;

            if (newStatus.Status == StatusEffect.Stunned && newStatus.Duration == 1)
                RpcAddEffectToDisplay(StatusEffect.Stunned, false);
                
            if (_statusEffects[i].Duration != 0) continue;
            
            if(_statusEffects.Count(e => e.Status == _statusEffects[i].Status) <= 1)
                RpcRemoveEffectToDisplay(_statusEffects[i].Status);
                
            _statusEffects.RemoveAt(i);
        }

        if (!_tookDamage) return;
        
        _tookDamage = false;
        RpcRemoveEffectToDisplay(StatusEffect.Shielded);

        var shieldIndex = _statusEffects.FindIndex(s => s.Status == StatusEffect.Shielded);
        if(shieldIndex >= 0) _statusEffects.RemoveAt(shieldIndex);
    }

    [Server]
    private void OnAttackExecuted(Attack attack)
    {
        if(!attack.Tiles.Contains(TilePosition))
            return;
        
        UpdateHealth(-attack.Damage);
    }
    
    [Server]
    private void UpdateHealth(int changeAmount)
    {
        var actualChange = changeAmount;

        if (HasEffectOfTypeActive(StatusEffect.Shielded))
            actualChange = Mathf.RoundToInt(actualChange / data.dmgReductionFromShield);
        
        _currentHealth = Mathf.Clamp(_currentHealth + actualChange, 0, data.health);

        if(actualChange >= 0) return;
        
        PlayHurtSound();
        _tookDamage = true;
        
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

    #region Networking
    
    [Command(requiresAuthority = false)]
    public void CmdUpdateHealth(int changeAmount)
    {
        UpdateHealth(changeAmount);
    }

    [Command(requiresAuthority = false)]
    private void CmdChangePosition(Vector3 position, Vector3Int tilePos)
    {
        RPCChangePosition(position, tilePos);
    }

    [Command(requiresAuthority = false)]
    public void CmdRegisterMoveIntent(MoveCommand moveCommand)
    {
        _pathManager.CreatePath(moveCommand,
            MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition);

        GameManager.Instance.RegisterMoveIntent(TilePosition, moveCommand);
        
        RPCAddToMoveIntent(moveCommand);
    }

    [Command(requiresAuthority = false)]
    public void CmdRegisterAttackIntent(Attack newAttack)
    {
        GameManager.Instance.RegisterAttackIntent(TilePosition, newAttack, owningTeam);
        RPCAddToAttackIntent(newAttack);
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateControlStatus(bool newState)
    {
        isControlled = newState;
    }
    
    [ClientRpc]
    private void RPCChangePosition(Vector3 position, Vector3Int tilePos)
    {
        transform.position = position;
        TilePosition = tilePos;
    }
    
    [ClientRpc]
    public void RPCStep(MoveCommand moveCommand)
    {
        StartCoroutine(MoveToPositions(moveCommand));
        
        GridManager.Instance.MoveUnit(TilePosition, moveCommand.TargetPosition);
        TilePosition = moveCommand.TargetPosition;
    }

    [ClientRpc]
    public void RPCCleanUp()
    {
        MoveIntent.Clear();
        AttackIntent.Clear();
    }

    [ClientRpc]
    private void RPCAddToMoveIntent(MoveCommand moveCommand)
    {
        MoveIntent.Add(moveCommand);
        UpdateActionDisplayAfterAction();
    }

    [ClientRpc]
    private void RPCAddToAttackIntent(Attack newAttack)
    {
        AttackIntent.Add(newAttack);
        UpdateActionDisplayAfterAction();
    }

    [ClientRpc]
    public void RPCExecuteAttack(Attack attack)
    {
        StartCoroutine(Attack(attack));
    }

    [ClientRpc]
    private void PlayHurtSound()
    {
        AudioManager.PlaySfx(_sfxSource, AudioManager.Instance.UnitHurt);
    }

    [ClientRpc]
    protected virtual void RpcDie()
    {
        GameManager.Instance.UnitDefeated(TilePosition);
        GridManager.Instance.RemoveUnit(TilePosition);
        Destroy(gameObject);
    }

    [Server]
    protected virtual IEnumerator Die()
    {
        _status = LiveStatus.Dead;
        
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
        RpcAddEffectToDisplay(newEffect.Status, newEffect.Status == StatusEffect.Stunned);
    }

    [Server]
    public void ServerAddNewStatusEffect(UnitStatus newEffect)
    {
        _statusEffects.Add(newEffect);
        RpcAddEffectToDisplay(newEffect.Status, false);
    }

    [ClientRpc]
    private void RpcAddEffectToDisplay(StatusEffect newEffect, bool checkForTeam)
    {
        if(!checkForTeam || GameManager.Instance.localPlayer.team != owningTeam)
            _effectDisplay.AddEffect(newEffect);
    }
    
    [ClientRpc]
    private void RpcRemoveEffectToDisplay(StatusEffect effect)
    {
        _effectDisplay.RemoveEffect(effect);
    }
    
    #endregion
}
