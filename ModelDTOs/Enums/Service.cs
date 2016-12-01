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
        BulkUpdate = 7,
        EntitiesUpdate = 8,
        UnitsUpdate = 9,
        ResourceProvidersUpdate = 10,
        ResourcesUpdate = 11,
        AddEntity = 12,
        RemoveEntity = 13,
        BulkRemoveEntities = 14,
        BulkAddEntities = 15,
        FetchOtherPlayers = 16,
        BeginAttack = 17,
        BattleState = 18,
        BattleEnd = 19,
        BattleStarted = 20,
    }
}