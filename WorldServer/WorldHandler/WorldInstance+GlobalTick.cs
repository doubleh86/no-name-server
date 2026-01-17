using WorldServer.JobModels;
using WorldServer.ResourcePool;

namespace WorldServer.WorldHandler;

public partial class WorldInstance
{
    private static readonly long _AutoSaveIntervalMs = 1000 * 60 * 5;
    private static readonly long _MonsterUpdateIntervalMs = 100;
    
    private long _lastCallAutoSaveTick = Environment.TickCount64;
    private long _lastCallMonsterUpdateTick = Environment.TickCount64;
    private int _autoSaving;
    
    public void Tick()
    {
        if(IsAliveWorld() == false) 
            return;

        var currentTick = Environment.TickCount64;
        _MonsterUpdateTick(currentTick);
        _AutoSaveTick(currentTick);
    }

    private void _AutoSaveTick(long currentTick)
    {
        if (currentTick - _lastCallAutoSaveTick < _AutoSaveIntervalMs)
            return;
        
        _lastCallAutoSaveTick = currentTick;
        _AutoSaveWorldState();
    }

    private void _MonsterUpdateTick(long currentTick)
    {
        if (Volatile.Read(ref _isChangingWorld) == 1)
            return;
        
        if (currentTick - _lastCallMonsterUpdateTick < _MonsterUpdateIntervalMs)
            return;
        
        _lastCallMonsterUpdateTick = currentTick;
        var centerCell = _worldMapInfo.GetCell(_worldOwner.GetPosition());
        if (centerCell == null)
            return;
        
        var nearByCells = _worldMapInfo.GetWorldNearByCells(_worldOwner.GetZoneId(), 
                                                            _worldOwner.GetPosition(), 
                                                            range: 2);

        var job = MonsterUpdateJobPool.Rent(_worldOwner.GetPosition(), nearByCells, _OnMonsterUpdate);
        _Push(job);
    }

}