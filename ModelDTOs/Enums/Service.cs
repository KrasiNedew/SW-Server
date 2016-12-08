namespace ModelDTOs.Enums
{
    public enum Service
    {
        None = 0,
        Info = 1,
        Login = 2,
        Logout = 3,
        Registration = 4,
        PlayerData = 5,
        UpdateEntities = 9,
        UpdateResourceSet = 11,
        AddEntity = 12,
        FetchOtherPlayers = 16,
        OtherPlayers,
        StartBattle = 17,
        BattleEnd = 19,
        BattleStarted = 20,
        Ping = 100,
    }
}