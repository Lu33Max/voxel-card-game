using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitEffectDisplay : MonoBehaviour
{
    [Serializable]
    private class StatusModel
    {
        public Unit.StatusEffect effect;
        public Mesh model;
        public Material material;
    }

    [SerializeField] private float distance = 0.4f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private StatusModel[] effectModels;
    [SerializeField] private Unit.StatusEffect[] hiddenEffects;

    private List<Unit.StatusEffect> _activeEffects = new();
    
    private GameObject[] _displays = new GameObject[4];
    private MeshFilter[] _meshFilters = new MeshFilter[4];
    private MeshRenderer[] _meshRenderers = new MeshRenderer[4];

    private Unit _unit = null!;
    
    private void Awake()
    {
        _unit = GetComponentInParent<Unit>();
        
        for (var i = 0; i < 4; i++)
        {
            _displays[i] = new GameObject();
            _meshFilters[i] = _displays[i].AddComponent<MeshFilter>();
            _meshRenderers[i] = _displays[i].AddComponent<MeshRenderer>();
            
            _displays[i].transform.SetParent(transform);
            _displays[i].transform.RotateAround(transform.position, Vector3.up, 90 * i);
        }
    }

    private void FixedUpdate()
    {
        foreach (var display in _displays)
        {
            display.transform.RotateAround(transform.position, Vector3.up, speed);
            display.transform.LookAt(transform.position);
        }
    }

    public void AddEffect(Unit.StatusEffect effect)
    {
        if(_activeEffects.Contains(effect) || (hiddenEffects.Contains(effect) && _unit.owningTeam == Player.LocalPlayer.team)) 
            return;
        
        _activeEffects.Add(effect);
        UpdateDisplay();
    }

    public void RemoveEffect(Unit.StatusEffect effect)
    {
        if(!_activeEffects.Contains(effect)) return;
        
        _activeEffects.Remove(effect);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_activeEffects.Count == 3)
            for (var i = 0; i < 3; i++)
            {
                _displays[i].transform.position = transform.position + Vector3.forward * distance;
                _displays[i].transform.RotateAround(transform.position, Vector3.up, 120 * i);
            }
        else
            for (var i = 0; i < 4; i++)
            {
                _displays[i].transform.position = transform.position + Vector3.forward * distance;
                _displays[i].transform.RotateAround(transform.position, Vector3.up, 90 * i);
            }

        var meshes = new Mesh[4];
        var materials = new Material[4];

        switch (_activeEffects.Count)
        {
            case 0:
                for (var i = 0; i < 4; i++)
                {
                    meshes[i] = null;
                    materials[i] = null;
                }
                break;
            
            case 1:
                var effectModel = effectModels.First(m => m.effect == _activeEffects[0]);
                
                for (var i = 0; i < 4; i++)
                {
                    if (i % 2 == 0)
                    {
                        meshes[i] = effectModel.model;
                        materials[i] = effectModel.material;
                        continue;
                    }
                    
                    meshes[i] = null;
                    materials[i] = null;
                }
                break;
            
            case 2:
                var effect1 = effectModels.First(m => m.effect == _activeEffects[0]);
                var effect2 = effectModels.First(m => m.effect == _activeEffects[1]);

                for (var i = 0; i < 4; i++)
                {
                    if (i % 2 == 0)
                    {
                        meshes[i] = effect1.model;
                        materials[i] = effect1.material;
                        continue;
                    }
                    
                    meshes[i] = effect2.model;
                    materials[i] = effect2.material;
                }
                break;
            
            case 3:
                var first = effectModels.First(m => m.effect == _activeEffects[0]);
                meshes[0] = first.model;
                materials[0] = first.material;
                
                var second = effectModels.First(m => m.effect == _activeEffects[1]);
                meshes[1] = second.model;
                materials[1] = second.material;
                
                var third = effectModels.First(m => m.effect == _activeEffects[2]);
                meshes[2] = third.model;
                materials[2] = third.material;

                meshes[3] = null;
                materials[3] = null;
                break;
            
            default:
                var eff1 = effectModels.First(m => m.effect == _activeEffects[0]);
                meshes[0] = eff1.model;
                materials[0] = eff1.material;
                
                var eff2 = effectModels.First(m => m.effect == _activeEffects[1]);
                meshes[1] = eff2.model;
                materials[1] = eff2.material;
                
                var eff3 = effectModels.First(m => m.effect == _activeEffects[2]);
                meshes[2] = eff3.model;
                materials[2] = eff3.material;

                var eff4 = effectModels.First(m => m.effect == _activeEffects[3]);
                meshes[3] = eff4.model;
                materials[3] = eff4.material;
                break;
        }

        for (var i = 0; i < 4; i++)
        {
            _meshFilters[i].mesh = meshes[i];
            _meshRenderers[i].material = materials[i];
        }
    }
}
