namespace Server.CommHandlers
{
    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.Constants;
    using Server.Services;

    using ServerUtils;

    public static class Parser
    {
        public static void ParseReceived(Client client, Message message)
        {
            switch (message.Service)
            {
                case Service.None:
                    Responses.SomethingWentWrong(client);
                    return;

                case Service.Login:
                    UserFull loginData = ((Message<UserFull>)message).Data;
                    client.User = loginData;
                    AuthenticationServices.Login(client);
                    break;

                case Service.Logout:
                    AuthenticationServices.Logout(client);
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
                        AuthenticationServices.Register(client);
                    }
                    else
                    {
                        Responses.AlreadyLoggedIn(client);
                    }

                    break;

                default:
                    Responses.ServiceNotRecognized(client);
                    break;
            }
        }
    }
}