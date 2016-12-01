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
                    if (!this.server.Users.IsLoggedIn(client.User))
                    {
                        UserFull loginData = ((Message<UserFull>)message).Data;
                        client.User = loginData;
                        this.server.Auth.Login(client);
                    }
                    else
                    {
                        this.server.Responses.AlreadyLoggedIn(client);
                    }
        
                    break;

                case Service.Logout:
                    this.server.Auth.Logout(client);
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
                        this.server.Auth.Register(client);
                    }
                    else
                    {
                        this.server.Responses.AlreadyLoggedIn(client);
                    }

                    break;

                default:
                    this.server.Responses.ServiceNotRecognized(client);
                    break;
            }
        }
    }
}