using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class Unit : NetworkBehaviour
{
    [SyncVar] public Team owningTeam = Team.None;
    [HideInInspector, SyncVar(hook = nameof(OnControlStatusChanged))] public bool isControlled;
    
    [Header("UnitData")]
    [SerializeField] protected UnitData data;
    [SerializeField] private int moveLimit = 3;
    [SerializeField] private int attackLimit = 1;
    
    [Header("Movement")] 
    [SerializeField] private float moveArcHeight = 0.1f;
    
    [Header("Visualization")]
    [SerializeField] private GameObject canvas;
    [SerializeField] protected Material outlineMaterial;
    
    [Header("Health Visualization")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthCounter;

    [Header("Shield Visualization")]
    [SerializeField] private Slider shieldSlider;
    [SerializeField] private TextMeshProUGUI shieldCounter;

    [Header("Unit Action Display")]
    [SerializeField] private Transform actionDisplayParent;
    [SerializeField] private GameObject actionImagePrefab;
    
    public Vector3Int TilePosition { get; private set; }
    protected List<MoveCommand> MoveIntent { get; } = new();
    public List<Attack> AttackIntent { get; } = new();
    public UnitData Data => data;
    public bool SetForSkip => _turnToSkip != GameState.Empty;

    [SyncVar(hook = nameof(OnHealthUpdated))] private int _currentHealth;
    [SyncVar(hook = nameof(OnShieldUpdated))] private int _currentShield;
    [SyncVar(hook = nameof(OnTurnSkipUpdated))] private GameState _turnToSkip = GameState.Empty;
    
    private Transform _camera;
    private MeshRenderer _renderer;
    private AudioSource _sfxSource;

    /// <summary>Get all tiles currently reachable by the unit. Only includes valid moves.</summary>
    /// <param name="movementRange">The movement range given by the played card</param>
    public abstract List<MoveCommand> GetValidMoves(int movementRange);

    public abstract List<Vector3Int> GetValidAttackTiles(int attackRange);
    
    [CanBeNull]
    public abstract Attack GetAttackForHoverPosition(Vector3Int hoveredPos, int attackRange, int damageMultiplier);

    private void Start()
    {
        if (Camera.main != null) 
            _camera = Camera.main.transform;
        _renderer = GetComponentInChildren<MeshRenderer>();
        _sfxSource = GetComponent<AudioSource>();
        
        GameManager.Instance.gameStateChanged.AddListener(OnGameStateChanged);
        
        healthSlider.GetComponent<HealthSlider>().SetupSliderColor(owningTeam);
        shieldSlider.GetComponent<HealthSlider>().SetupSliderColor(owningTeam);

        if(!isServer)
            return;
        
        UpdateHealth(data.health);
        GameManager.AttackExecuted.AddListener(OnAttackExecuted);
        GameManager.CheckHealth.AddListener(OnCheckHealth);
    }

    private void Update()
    {
        canvas.transform.LookAt(_camera);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        GameManager.Instance.gameStateChanged.RemoveListener(OnGameStateChanged);
        
        if(!isServer)
            return;
        
        GameManager.AttackExecuted.RemoveListener(OnAttackExecuted);
        GameManager.CheckHealth.RemoveListener(OnCheckHealth);
    }

    /// <summary>Instantly move the unit to the given tile</summary>
    public void MoveToTile(Vector3Int tilePos)
    {
        Vector3? worldPos = GridManager.Instance.GridToWorldPosition(tilePos);
        
        if(worldPos != null)
            CmdChangePosition(worldPos.Value, tilePos);
    }

    /// <summary>Step to a target tile while passing over all the given tiles in the path</summary>
    public void StepToTile(MoveCommand moveCommand)
    {
        //StartCoroutine(MoveToPositions(moveCommand));
        CmdStep(moveCommand);
        GridManager.Instance.MoveUnit(TilePosition, moveCommand.TargetPosition);
    }

    public void LogMovement(CardData cardValues, MoveCommand move)
    {
        GameManager.Instance.CmdLogAction(GameManager.Instance.localPlayer.netId.ToString(), 
            owningTeam.ToString(), "move", $"[{cardValues.movementRange}]", move.TargetPosition.ToString(), 
            gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());
    }
    
    public void LogAttack(CardData cardValues, Attack attack)
    {
        GameManager.Instance.CmdLogAction(GameManager.Instance.localPlayer.netId.ToString(), 
            owningTeam.ToString(), "attack", $"[{cardValues.attackRange},{cardValues.attackDamage}]", $"[{string.Join(",", attack.Tiles.Select(t => t.ToString()).ToList())}]", 
            gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());
    }

    public bool CanBeSelected()
    {
        var state = GameManager.Instance.gameState;
        
        return ((state == GameState.Attack && AttackIntent.Count < attackLimit) ||
               (state == GameState.Movement && MoveIntent.Count < moveLimit)) && _turnToSkip != state;
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
    protected virtual IEnumerator Attack(Attack attack)
    {
        Vector3 startingPos = transform.position;
        Vector3 direction = GridManager.Instance.GridToWorldPosition(attack.Tiles[0])!.Value - startingPos;
        direction.y = 0;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
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
    
    private void OnShieldUpdated(int old, int newShield)
    {
        if (newShield <= 0)
        {
            shieldSlider.gameObject.SetActive(false);
            return;
        }
            
        shieldSlider.gameObject.SetActive(true);

        shieldSlider.value = (float)newShield / data.health;
        shieldCounter.text = newShield.ToString();
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

        if (_turnToSkip != GameState.Empty)
        {
            var newDisplay = Instantiate(actionImagePrefab, actionDisplayParent);
            newDisplay.GetComponent<ActionDisplayImage>().SetIcon(ActionDisplayType.Stun);
        }
        else if(newState == GameState.Movement)
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
        if(isServer)
            CheckForSkipMove(newState);
        
        UpdateActionDisplayOnStateUpdate(newState);
    }

    [Server]
    private void CheckForSkipMove(GameState newState)
    {
        if(newState != GameState.Attack && newState != GameState.Movement)
            return;
        
        if (newState == _turnToSkip)
            return;
        
        _turnToSkip = GameState.Empty;
    }

    private void OnTurnSkipUpdated(GameState old, GameState newTurnToSkip)
    {
        if (owningTeam == GameManager.Instance.localPlayer.team)
        {
            if (newTurnToSkip == GameState.Empty)
                UpdateActionDisplayOnStateUpdate(GameManager.Instance.gameState);
            
            return;
        }

        if (newTurnToSkip == GameState.Empty && actionDisplayParent.childCount > 0)
        {
            Destroy(actionDisplayParent.GetChild(0).gameObject); 
            return;
        }

        var newDisplay = Instantiate(actionImagePrefab, actionDisplayParent);
        newDisplay.GetComponent<ActionDisplayImage>().SetIcon(ActionDisplayType.Stun);
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
        var changeLeft = changeAmount;

        if (_currentShield > 0 && changeAmount < 0)
        {
            if (_currentShield >= Mathf.Abs(changeAmount))
            {
                _currentShield += changeAmount;
                ActionLogger.Instance.LogAction("server", owningTeam.ToString(), "shield_damaged", $"[{changeAmount},{_currentShield}]", 
                    null, gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());
                
                PlayHurtSound();
                return;
            }
            
            ActionLogger.Instance.LogAction("server", owningTeam.ToString(), "shield_damaged", $"[{_currentShield},{_currentHealth}]", 
                null, gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());

            changeLeft = changeAmount + _currentShield;
            _currentShield = 0;
        }
        
        _currentHealth = Mathf.Clamp(_currentHealth + changeLeft, 0, data.health);

        if (changeAmount < 0)
        {
            PlayHurtSound();
            ActionLogger.Instance.LogAction("server", owningTeam.ToString(), "damaged", $"[{changeLeft},{_currentHealth}]", 
                null, gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());
        }
        else if(changeAmount > 0)
            ActionLogger.Instance.LogAction("server", owningTeam.ToString(), "heal", $"[{changeLeft},{_currentHealth}]", 
                null, gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());
    }

    [Server]
    public void AddShield(int shieldToAdd)
    {
        _currentShield = Mathf.Clamp(_currentShield + shieldToAdd, 0, data.health);
        ActionLogger.Instance.LogAction("server", owningTeam.ToString(), "shield_add", $"[{shieldToAdd},{_currentShield}]", 
            null, gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());
    }

    [Server]
    protected void OnCheckHealth()
    {
        if (_currentHealth > 0) 
            return;
        
        //Logging
        ActionLogger.Instance.LogAction("server", owningTeam.ToString(), "died", null, 
            null, gameObject.GetInstanceID().ToString(), data.unitName, TilePosition.ToString());
        
        Die();
    }

    #region Networking
    
    [Command(requiresAuthority = false)]
    public void CmdUpdateHealth(int changeAmount)
    {
        UpdateHealth(changeAmount);
    }
    
    [Command(requiresAuthority = false)]
    public void CmdUpdateShield(int changeAmount)
    {
        AddShield(changeAmount);
    }

    [Command(requiresAuthority = false)]
    private void CmdChangePosition(Vector3 position, Vector3Int tilePos)
    {
        RPCChangePosition(position, tilePos);
    }

    [Command(requiresAuthority = false)]
    private void CmdStep(MoveCommand moveCommand)
    {
        RPCStep(moveCommand);
    }

    [Command(requiresAuthority = false)]
    public void CmdRegisterMoveIntent(MoveCommand moveCommand)
    {
        PathManager.Instance.CreatePath(moveCommand,
            MoveIntent.Count > 0 ? MoveIntent.Last().TargetPosition : TilePosition, TilePosition);

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

    [Command(requiresAuthority = false)]
    public void CmdUpdateTurnSkip()
    {
        _turnToSkip = GameManager.Instance.gameState == GameState.Movement ? GameState.Attack : GameState.Movement;
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
    protected virtual void Die()
    {
        GameManager.Instance.UnitDefeated(TilePosition);
        GridManager.Instance.RemoveUnit(TilePosition);
        Destroy(gameObject);
    }

    #endregion
}
