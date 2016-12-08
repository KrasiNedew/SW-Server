namespace Server.CommHandlers.Interfaces
{
    using ModelDTOs;

    using ServerUtils.Wrappers;

    public interface Writer
    {
        void SendTo(Client client, Message message);

        void SendToThenDropConnection(Client client, Message message);
    }
}