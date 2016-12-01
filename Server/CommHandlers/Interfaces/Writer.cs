namespace Server.CommHandlers.Interfaces
{
    using ModelDTOs;

    using ServerUtils.Wrappers;

    public interface Writer
    {
        void SendTo(Client client, Message message);

        void SendFromTo(Client sender, Message message, params Client[] receivers);

        void SendToThenDropConnection(Client client, Message message);

        void BroadcastToAll(Message message);


    }
}