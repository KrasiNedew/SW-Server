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

                case Service.AddResProv:
                    this.server.Game.AddResProv(client, message);
                    break;
                case Service.AddUnit:
                    this.server.Game.AddUnit(client, message);
                    break;
                case Service.UpdateEntities:
                    this.server.Game.UpdateEntities(client, message);
                    break;
                case Service.UpdateResourceSet:
                    this.server.Game.UpdateResources(client, message);
                    break;

                case Service.FetchOtherPlayers:
                    this.server.Game.SendOtherPlayers(client);
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