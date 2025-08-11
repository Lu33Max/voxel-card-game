using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "StageEvents/FloodEvent")]
public class FloodEvent : StageEventBase
{
    [Serializable]
    public class FloodParameters : StageEventParameters
    {
        public int floodHeight = 1;
        
        /// <summary> Units to rise per second </summary>
        public float risingSpeed = 0.5f;
    }

    private GameObject _water;
    private float _originalHeight;

    public override void Setup()
    {
        _water = GameObject.FindGameObjectWithTag("Water");
        _originalHeight = _water.transform.position.y;
    }
    
    public override void Execute(StageEventParameters parameters, MonoBehaviour runner)
    {
        var p = (FloodParameters)parameters;
        
        // Get all effected tiles
        var effectedTiles = GridManager.Instance.GetTilesFiltered(tile => tile.TilePosition.y < p.floodHeight);

        foreach (var tile in effectedTiles)
        {
            tile.State = TileData.TileState.Flooded;
            
            if(tile.Unit != null)
                tile.Unit.CmdUpdateHealth(-999);
        }

        runner.StartCoroutine(RiseWater(p.floodHeight, p.risingSpeed));
    }

    public override StageEventParameters CreateDefaultParameters()
    {
        return new FloodParameters();
    }

    private IEnumerator RiseWater(int floodHeight, float speed)
    {
        var tileHeight = GridManager.Instance.TileSize.y;
        
        var targetHeight = _originalHeight + floodHeight * tileHeight;

        while (_water.transform.position.y < targetHeight)
        {
            var waterPosition = _water.transform.position;
            
            _water.transform.position = new Vector3(waterPosition.x, waterPosition.y + tileHeight * speed * Time.deltaTime,
                waterPosition.z);

            yield return new WaitForEndOfFrame();
        }

        var finalPosition = _water.transform.position;
        finalPosition.y = targetHeight;

        _water.transform.position = finalPosition;
    }
}
