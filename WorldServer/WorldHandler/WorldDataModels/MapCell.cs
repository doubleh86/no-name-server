using System.Numerics;
using WorldServer.GameObjects;

namespace WorldServer.WorldHandler.WorldDataModels;

public class MapCell : IDisposable
{
    public int ZoneId { get; set; }
    public int X { get; }
    public int Z { get; }
    
    public Vector3 WorldPosition { get; private set; }

    private readonly Dictionary<long, GameObject> _mapObjects = new();
    private readonly Dictionary<long, PlayerObject> _players = new();
    
    public Dictionary<long, GameObject> GetMapObjects() => _mapObjects;
    
    
    public MapCell(int zoneId, int x, int z, Vector3 worldOffset, int chunkSize)
    {
        ZoneId = zoneId;
        X = x;
        Z = z;
        
        WorldPosition = new Vector3(worldOffset.X + (X * chunkSize) + (chunkSize * 0.5f), 
                                    0, 
                                    worldOffset.Z + (Z * chunkSize) + (chunkSize * 0.5f));
    }

    public void Enter(GameObject obj)
    {
        _mapObjects.TryAdd(obj.GetId(), obj);
        if(obj is PlayerObject player)
            _players.TryAdd(player.GetId(), player);
        
    }

    public void Leave(long objId)
    {
        _mapObjects.Remove(objId, out _);
        _players.Remove(objId, out _);
    }

    public void Dispose()
    {
        _mapObjects.Clear();
        _players.Clear();
    }
}