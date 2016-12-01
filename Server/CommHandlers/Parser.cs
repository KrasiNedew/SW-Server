namespace Server.CommHandlers
{
    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.Services;

    using ServerUtils.Wrappers;

    public static class Parser
    {
        public static void ParseReceived(this AsynchronousSocketListener server, Client client, Message message)
        {
            switch (message.Service)
            {
                case Service.None:
                    server.SomethingWentWrong(client);
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
                        server.AlreadyLoggedIn(client);
                    }

                    break;

                default:
                    server.ServiceNotRecognized(client);
                    break;
            }
        }
    }
}