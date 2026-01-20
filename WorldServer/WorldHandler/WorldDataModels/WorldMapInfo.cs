using System.Numerics;
using DataTableLoader.Models;
using DataTableLoader.Helper;
using ServerFramework.CommonUtils.Helper;
using WorldServer.GameObjects;

namespace WorldServer.WorldHandler.WorldDataModels;

/// <summary>
/// chunk_size 1 => 1m
/// size_x 1000 이면 1km
/// MaxChunkSize = (size_x / chunk_size) (2000 / 25 => 80)
/// 예시 ( size_x 2000, chunk_size 25 => MaxChunkSize 80)
/// Cell Size ( 25, 25 )
/// Zone Size ( 80, 80 )
/// Cell total count : 80 * 80 = 6400
/// 
/// </summary>
public class WorldMapInfo : MapInfoBase
{
    private const float _MinSpawnDist = 2.5f;

    private readonly long _accountId;
    private readonly List<MonsterGroup> _monsterGroups = new();
    
    public int GetWorldMapId() => _worldMapId;
    public List<MonsterGroup> GetMonsterGroups() => _monsterGroups;
    public WorldMapInfo(long accountId, LoggerService loggerService) : base(loggerService)
    {
        _accountId = accountId;
    }
    
    public override void Initialize(int worldMapId)
    {
        if(_zoneCells.Count > 0 || _monsterGroups.Count > 0 || _worldMapId != 0)
            ClearWorld();
        
        base.Initialize(worldMapId);
        _InitializeMonsters();
    }

    private void _InitializeMonsters()
{
    var monsterGroupList = DataTableHelper.GetDataList<MonsterTGroup>()
                                          .Where(x => x.world_id == _worldMapId);

    var rng = new Random(); // 서버 전체에서 1개만 만들어서 쓰는 게 좋음

    foreach (var monsterGroup in monsterGroupList)
    {
        var registerMonsterGroup = new MonsterGroup(monsterGroup, IdGenerator.NextId(_accountId));
        var zoneId = _FindZoneByWorldPosition(registerMonsterGroup.AnchorPosition);
        if (zoneId == -1)
        {
            _loggerService.Warning($"Can't find zone for monster {monsterGroup.monster_group_id}");
            continue;
        }
        registerMonsterGroup.SetZoneId(zoneId);
        
        // 겹침 방지용
        var usedPositions = new List<Vector3>(monsterGroup.MonsterList.Count);
        
        foreach (var monsterId in monsterGroup.MonsterList)
        {
            var tableData = DataTableHelper.GetData<MonsterInfo>(monsterId);
            if (tableData == null)
                continue;

            Vector3 spawnPos = registerMonsterGroup.AnchorPosition;
            Vector3 anchorPos = registerMonsterGroup.AnchorPosition;
            bool found = false;

            // 최대 15번 시도해서 분산 스폰 찾기
            for (int i = 0; i < 15; i++)
            {
                var candidate = _GetRandomSpawnPosition(anchorPos, registerMonsterGroup.GetRoamRadius(), rng);

                // zone 안인지 / cell 유효한지 체크
                var cell = GetCell(candidate);
                if (cell == null)
                    continue;

                // 몬스터끼리 너무 붙지 않게
                if (_IsFarEnough(candidate, usedPositions, _MinSpawnDist) == false)
                    continue;

                spawnPos = candidate;
                usedPositions.Add(candidate);
                found = true;
                break;
            }

            // 못 찾으면 앵커에라도 스폰(최후)
            if (found == false)
            {
                var cell = GetCell(anchorPos);
                if (cell == null)
                    break;
                spawnPos = anchorPos;
            }

            var targetCell = GetCell(spawnPos);
            if (targetCell == null)
                break;

            var spawnedMonster = new MonsterObject(
                IdGenerator.NextId(_accountId),
                spawnPos,
                zoneId,
                registerMonsterGroup,
                tableData);

            targetCell.Enter(spawnedMonster);
            registerMonsterGroup.AddMember(spawnedMonster);
        }

        if (registerMonsterGroup.MonsterCount < 1)
            continue;

        _monsterGroups.Add(registerMonsterGroup);
    }
}


    // Player AOI
    public (List<MapCell>, List<MapCell>) UpdatePlayerView(int oldZoneId, int newZoneId, Vector3 oldPosition, Vector3 newPosition)
    {
        if (oldZoneId == -1 || newZoneId == -1)
            return ([], []);
        
        var oldNearByCells = GetWorldNearByCells(oldZoneId, oldPosition, range: 2);
        var nearByCells = GetWorldNearByCells(newZoneId, newPosition, range: 2);

        var oldSet = oldNearByCells.ToHashSet();
        var newSet = nearByCells.ToHashSet();
        
        var enter = new List<MapCell>();
        foreach (var cell in newSet)
        {
            if(oldSet.Contains(cell) == false)
                enter.Add(cell);
        }
        
        var leave = new List<MapCell>();
        foreach (var cell in oldSet)
        {
            if(newSet.Contains(cell) == false)
                leave.Add(cell);
        }
        
        return (enter, leave);
    }

    // world 이동 시
    public override void ClearWorld()
    {
        base.ClearWorld();
        _monsterGroups.Clear();
    }
    
    private static bool _IsFarEnough(Vector3 p, List<Vector3> used, float minDist)
    {
        float minDistSq = minDist * minDist;
        foreach (var u in used)
        {
            var d = p - u;
            if (d.LengthSquared() < minDistSq)
                return false;
        }
        return true;
    }

    
    private static Vector3 _GetRandomSpawnPosition(Vector3 anchor, float radius, Random random)
    {
        // 원 안에서 균등 분포: r = sqrt(u) * R
        float u = (float)random.NextDouble();
        float v = (float)random.NextDouble();
        float r = MathF.Sqrt(u) * radius;
        float theta = v * MathF.PI * 2f;

        float x = anchor.X + MathF.Cos(theta) * r;
        float z = anchor.Z + MathF.Sin(theta) * r;

        return new Vector3(x, anchor.Y, z);
    }


}