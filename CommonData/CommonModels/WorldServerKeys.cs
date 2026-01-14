namespace CommonData.CommonModels;

public enum WorldServerKeys : int
{
    RequestStart = 2000,
    RequestWorldJoin = 2001,
    GameCommandRequest = 2002,
    RequestEnd,
    
    ResponseStart = 3000,
    ResponseWorldJoin = 3001,
    GameCommandResponse = 3002,
    ResponseEnd,
}