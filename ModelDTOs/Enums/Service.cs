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
        SenderUsername = 6,
        FullUpdate = 666,
        UpdateUnits = 9,
        UpdateResourceProviders = 10,
        UpdateResourceSet = 11,
        AddEntity = 12,
        RemoveEntity = 13,
        FetchOtherPlayers = 16,
        StartBattle = 17,
        BattleEnd = 19,
        BattleStarted = 20,
        Ping = 100,
    }
}