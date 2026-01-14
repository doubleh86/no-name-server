using System.Numerics;
using CommonData.CommonModels.Enums;
using CommonData.NetworkModels.WorldServerProtocols.GameProtocols;
using DbContext.GameDbContext.DbResultModel;
using WorldServer.Network;

namespace WorldServer.GameObjects;

public class PlayerObject : GameObject, IAutoSavable
{
    private static readonly float _AutoSaveDistanceMeters = 150f;
    private static readonly float AutoSaveDistanceSq = _AutoSaveDistanceMeters * _AutoSaveDistanceMeters;
    private int _saveDirty;
    
    private readonly UserSessionInfo _sessionInfo;
    private PlayerInfoResult _playerInfo;
    
    public UserSessionInfo GetSessionInfo() => _sessionInfo;
    public long AccountId => _playerInfo.account_id;

    private Vector3 _lastSavedPosition = new(float.NaN, float.NaN, float.NaN);
    private int _lastSavedZoneId = 0;
    
    public PlayerObject(long id, Vector3 position, UserSessionInfo sessionInfo, int zoneId) 
        : base(id, zoneId, position, GameObjectType.Player)
    {
        _sessionInfo = sessionInfo;
    }

    public void SetPlayerInfo(PlayerInfoResult playerInfo)
    {
        _playerInfo = playerInfo;
        
        var position = new Vector3(_playerInfo.position_x, _playerInfo.position_y, _playerInfo.position_z);
        UpdatePosition(position, 0f, playerInfo.last_zone_id);
        
        _lastSavedPosition = position;
        _lastSavedZoneId = playerInfo.last_zone_id;
    }

    public override void UpdatePosition(Vector3 position, float rotation, int zoneId)
    {
        _position = position;
        _rotation = rotation;
        _zoneId = zoneId;

        if (_MoveDistanceDirtyCheck() == false)
            return;
        
        MarkSaveDirty();
    }

    private bool _MoveDistanceDirtyCheck()
    {
        if (float.IsNaN(_lastSavedPosition.X) == true)
            return true;

        if (_lastSavedZoneId != GetZoneId())
            return true;
        
        var delta = GetPosition() - _lastSavedPosition;
        float distanceSquared = delta.LengthSquared();
        
        return distanceSquared >= AutoSaveDistanceSq;
    }
    
    public PlayerInfoResult GetPlayerInfoWithSave(bool isAutoSave = false)
    {
        if(isAutoSave == false)
            return _playerInfo;
        
        // sync 를 맞춘다.
        _playerInfo.last_zone_id = GetZoneId();
        _playerInfo.position_x = GetPosition().X;
        _playerInfo.position_y = GetPosition().Y;
        _playerInfo.position_z = GetPosition().Z;
        
        return _playerInfo;
    }

    public override GameObjectBase ToPacket()
    {
        return new GameObjectBase()
        {
            Id = AccountId,
            ZoneId = GetZoneId(),
            Position = GetPosition(),
            Type = GameObjectType.Player
        };
    }

    public bool IsSaveDirty()
    {
        return Volatile.Read(ref _saveDirty) == 1;
    }

    public void ClearSaveDirty()
    {
        Interlocked.Exchange(ref _saveDirty, 0);
    }

    public void MarkSaveDirty()
    {
        if (IsSaveDirty() == true)
            return;
        
        Interlocked.Exchange(ref _saveDirty, 1);
    }

    public void OnAutoSaveSuccess()
    {
        _lastSavedPosition = GetPosition();
        _lastSavedZoneId = GetZoneId();
        
        ClearSaveDirty();
    }
}