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
                    server.Responses.SomethingWentWrong(client);
                    return;

                case Service.Login:
                    UserFull loginData = ((Message<UserFull>)message).Data;
                    client.User = loginData;
                    server.Auth.Login(client);
                    break;

                case Service.Logout:
                    server.Auth.Logout(client);
                    break;

                case Service.Registration:
                    UserFull user = ((Message<UserFull>)message).Data;
                    if (client.User == null 
                        || !client.User.LoggedIn 
                        || client.User.Id == 0 
                        || string.IsNullOrEmpty(client.User.Username) 
                        || string.IsNullOrEmpty(client.User.PasswordHash))
                    {
                        client.User = user;
                        server.Auth.Register(client);
                    }
                    else
                    {
                        server.Responses.AlreadyLoggedIn(client);
                    }

                    break;

                default:
                    server.Responses.ServiceNotRecognized(client);
                    break;
            }
        }
    }
}