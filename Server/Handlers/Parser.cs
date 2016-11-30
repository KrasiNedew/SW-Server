namespace Server.Handlers
{
    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.Constants;
    using Server.Services;
    using Server.Wrappers;

    public static class Parser
    {
        public static void ParseReceived(Client client, Message message)
        {
            int err;
            switch (message.Service)
            {
                case Service.None:
                    Writer.SendToThenDropConnection(client, new Message<string>(Service.Info, Messages.InternalErrorDrop));
                    return;
                case Service.Login:
                    UserFull loginData = ((Message<UserFull>)message).Data;
                    client.User = loginData;
                    err = AuthenticationServices.Login(client);

                    switch (err)
                    {
                        case 0:
                            Writer.SendTo(client, new Message<string>(Service.Info, Messages.LoginSuccess));
                            break;
                        case ErrorCodes.InvalidCredentialsError:
                            Writer.SendTo(client, new Message<string>(Service.Info, Messages.InvalidCredentials));
                            break;
                        case ErrorCodes.AlreadyLoggedIn:
                            Writer.SendTo(
                                client, new Message<string>(Service.Info, Messages.PlayerAlreadyLoggedIn));
                            break;
                        default:
                            Writer.SendToThenDropConnection(
                                client, new Message<string>(Service.Info, Messages.InternalErrorDrop));
                            return;
                    }

                    break;

                case Service.Logout:
                    err = AuthenticationServices.Logout(client);
                    switch (err)
                    {
                        case 0:
                            Writer.SendTo(client,
                                new Message<string>(Service.Info, Messages.LogoutSuccess));
                            break;
                        case ErrorCodes.LogoutError:
                            Writer.SendTo(client, new Message<string>
                                (Service.Info, Messages.DataNotSaved));
                            break;
                        default:
                            Writer.SendToThenDropConnection(
                                client, new Message<string>
                                (Service.Info, Messages.InternalErrorDrop));
                            return;
                    }

                    break;

                case Service.Registration:
                    UserFull user = ((Message<UserFull>)message).Data;
                    if (client.User == null || !client.User.LoggedIn || client.User.Id == 0 || string.IsNullOrEmpty(client.User.Username) || string.IsNullOrEmpty(client.User.PasswordHash))
                    {
                        client.User = user;
                        err = AuthenticationServices.Register(client);
                    }
                    else
                    {
                        err = ErrorCodes.AlreadyLoggedIn;
                    }

                    switch (err)
                    {
                        case 0:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Info, Messages.RegisterSuccessful));
                            break;
                        case ErrorCodes.AlreadyLoggedIn:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Info, Messages.AlreadyLoggedIn));
                            break;
                        case ErrorCodes.UsernameEmptyError:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Info, Messages.EmptyUsername));
                            break;
                        case ErrorCodes.PasswordEmptyError:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Info, Messages.EmptyPassword));
                            break;
                        case ErrorCodes.UsernameTakenError:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Info, Messages.UsernameTaken));
                            break;
                        default:
                            Writer.SendToThenDropConnection(
                            client,
                            new Message<string>(Service.Info, Messages.InternalErrorDrop));
                            return;
                    }

                    break;
            }
        }
    }
}