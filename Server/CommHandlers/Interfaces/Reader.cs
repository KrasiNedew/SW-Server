namespace Server.CommHandlers.Interfaces
{
    using ServerUtils.Wrappers;

    public interface Reader
    {
        void ReadSingleMessage(Client client);

        void ReadMessagesContinuously(Client client);
    }
}