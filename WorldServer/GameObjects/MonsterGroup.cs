using System.Numerics;
using DataTableLoader.Models;
using NetworkProtocols.Socket.WorldServerProtocols.Models;

namespace WorldServer.GameObjects;

public class MonsterGroup
{
    private long _id { get; set; }
    private MonsterTGroup _monsterTGroup { get; set; }
    
    private readonly List<MonsterObject> _monsters = new();
    
    public Vector3 AnchorPosition => _monsterTGroup.AnchorPosition;
    private int _zoneId;
    
    public bool IsAnyMemberInCombat { get; set; }
    public PlayerObject TargetPlayer { get; set; }
    
    public int MonsterCount => _monsters.Count;
    public List<MonsterObject> Monsters => _monsters;

    public MonsterGroup(MonsterTGroup monsterTGroup, long id)
    {
        _id = id;
        _monsterTGroup = monsterTGroup.Clone() as MonsterTGroup;
    }

    public void SetZoneId(int zoneId)
    {
        _zoneId = zoneId;
    }
    
    public float GetRoamRadius() => _monsterTGroup.roam_radius;

    public void AddMember(MonsterObject spawnedMonster)
    {
        _monsters.Add(spawnedMonster);
    }
    
    public void UpdateIsAnyMemberInCombat(PlayerObject targetPlayer)
    {
        IsAnyMemberInCombat = _monsters.Any(monster => monster.GetState() == AIState.Attack 
                                                       || monster.GetState() == AIState.Chase);
        if (IsAnyMemberInCombat == false)
        {
            TargetPlayer = null;
            return;
        }
            
        TargetPlayer = targetPlayer;
    }
}