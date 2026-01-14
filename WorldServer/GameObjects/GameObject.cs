using System.Numerics;
using CommonData.CommonModels.Enums;
using CommonData.NetworkModels.WorldServerProtocols.GameProtocols;

namespace WorldServer.GameObjects;

public abstract class GameObject
{
    protected readonly long _id;
    private readonly GameObjectType _objectType; // 0: Player, 1: Monster, 2: StaticObject, 3: DynamicObject
    protected Vector3 _position;
    protected float _rotation;
    
    private Vector3 _changePosition = Vector3.Zero;
    private float _changeRotation = 0f;
    protected int _zoneId;
    
    protected bool _isChanged = false;
    public bool IsChanged() => _isChanged;

    public long GetId() => _id;
    public Vector3 GetPosition() => _position;
    public Vector3 GetChangePosition() => _changePosition;
    
    public int GetZoneId() => _zoneId;
    public float GetRotation() => _rotation;
    public float GetChangeRotation() => _changeRotation;
    public abstract GameObjectBase ToPacket();
    protected GameObject(long id, int zoneId, Vector3 position, GameObjectType objectType)
    {
        _id = id;
        _position = position;
        _zoneId = zoneId;
        _objectType = objectType;
    }

    public virtual void UpdatePosition(Vector3 position, float rotation, int zoneId)
    {
        _position = position;
        _rotation = rotation;
        _zoneId = zoneId;
        
        _UpdateChangePositionAndRotation(Vector3.Zero, 0f);
    }

    protected virtual void _UpdateChangePositionAndRotation(Vector3 changePosition, float rotation)
    {
        _changePosition = changePosition;
        _changeRotation = rotation;
        
        _isChanged = true;
    }
    
    public void ResetChanged() => _isChanged = false;
}