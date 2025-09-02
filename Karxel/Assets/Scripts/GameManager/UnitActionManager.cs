// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Mirror;
// using UnityEngine;
// using Random = UnityEngine.Random;
//
// [RequireComponent(typeof(NetworkIdentity))]
// public class UnitActionManager : NetworkBehaviour
// {
//     public class Cache<T> : Dictionary<int, T> where T: UndoElement
//     {
//         public new void Add(int key, T element)
//         {
//             if (Count >= 5)
//             {
//                 var first = this.OrderBy(e => e.Value.CreatedAt).First().Key;
//                 Remove(first);
//             }
//             
//             base.Add(key, element);
//         }
//     }
//     
//     public class UndoElement
//     {
//         public DateTime CreatedAt;
//     }
//     
//     public static UnitActionManager Instance { get; private set; }
//     
//     private Dictionary<Vector3Int, List<MoveCommand>> _moveIntents = new();
//     private Dictionary<int, UndoElement> _undoCache = new();
//     
//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }
//         
//         Instance = this;
//     }
//
//     [Client]
//     public void RegisterMoveIntent(Unit unit, MoveCommand moveCommand)
//     {
//         unit.UpdateMoveIntent(moveCommand); // Update method first called on sender, later on all other clients. Adds command to local MoveIntent and updates displayed path
//         HandManager.Instance.PlaySelectedCard();
//         
//         // Store changes inside cache with unique actionId
//         var actionId = Random.Range(int.MinValue, int.MaxValue);
//         _undoCache.Add(actionId, new UndoElement{ CreatedAt = DateTime.Now, Unit = unit });
//         
//         CmdRegisterMoveIntent(unit.TilePosition, moveCommand, unit.Id, actionId);
//     }
//
//     [Command(requiresAuthority = false)]
//     private void CmdRegisterMoveIntent(Vector3Int unitPos, MoveCommand moveCommand, int unitId, int actionId, NetworkConnectionToClient sender = null)
//     {
//         var unitTile = GridManager.Instance.GetTileAtGridPosition(unitPos);
//
//         if (unitTile == null || unitTile.Unit == null /*|| unitTile.Unit.Id != unitId */ ||
//             !GridManager.Instance.IsMoveValid(moveCommand))
//         {
//             TargetResetMoveIntent(sender, actionId);
//             return;
//         }
//         
//         // Check if player really has the current card in their inventory
//             
//         if (_moveIntents.TryGetValue(unitPos, out _))
//             _moveIntents[unitPos].Add(moveCommand);
//         else
//             _moveIntents.Add(unitPos, new List<MoveCommand>{ moveCommand });
//     }
//
//     [TargetRpc]
//     private void TargetResetMoveIntent(NetworkConnectionToClient client, int actionId)
//     {
//         if(!_undoCache.TryGetValue(actionId, out var undoElement))
//             return; // TODO: If no action was saved → Manipulation → Disconnect client
//
//         undoElement.Unit.RevokeMoveIntent();
//     }
//     
//     private void AddTo
// }
