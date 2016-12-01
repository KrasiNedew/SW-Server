namespace Server.CommHandlers.Interfaces
{
    using ModelDTOs;

    using ServerUtils.Wrappers;

    public interface Parser
    {
        void ParseReceived(Client client, Message message);
    }
}