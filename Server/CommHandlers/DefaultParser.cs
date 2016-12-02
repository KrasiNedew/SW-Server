namespace Server.CommHandlers
{
    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.CommHandlers.Interfaces;

    using ServerUtils.Wrappers;

    public class DefaultParser : Parser
    {
        private readonly AsynchronousSocketListener server;

        public DefaultParser(AsynchronousSocketListener server)
        {
            this.server = server;
        }

        public void ParseReceived(Client client, Message message)
        {
            switch (message.Service)
            {
                case Service.None:
                    this.server.Responses.SomethingWentWrong(client);
                    return;

                case Service.Login:
                    this.server.Auth.Login(client, message);
                    break;
                case Service.Logout:
                    this.server.Auth.Logout(client);
                    break;
                case Service.Registration:
                    this.server.Auth.Register(client, message);
                    break;

                case Service.FullUpdate:
                    this.server.Game.UpdateFull(client, message);
                    break;
                case Service.UpdateUnits:
                    this.server.Game.UpdateUnits(client, message);
                    break;
                case Service.UpdateResourceProviders:
                    this.server.Game.UpdateResourceProviders(client, message);
                    break;
                case Service.UpdateResourceSet:
                    this.server.Game.UpdateResources(client, message);
                    break;

                case Service.StartBattle:
                    this.server.Game.StartBattle(client, message);
                    break;
                case Service.BattleEnd:
                    this.server.Game.EndBattle(client);
                    break;

                default:
                    this.server.Responses.ServiceNotRecognized(client);
                    break;
            }
        }
    }
}