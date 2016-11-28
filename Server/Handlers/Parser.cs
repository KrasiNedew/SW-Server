namespace Server.Handlers
{
    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.Wrappers;

    using ServerUtils;

    public static class Parser
    {
        public static void ParseReceived(Client client, Message message)
        {
            int err;
            switch (message.Service)
            {
                case Service.None:
                    Writer.SendToThenDropConnection(client, new Message<string>(Service.Login, Messages.InternalErrorDrop));
                    return;
                case Service.Login:
                    AuthDataRaw authDataRaw = ((Message<AuthDataRaw>)message).Data;
                    AuthDataSecure authDataSecure = new AuthDataSecure(authDataRaw.Username, authDataRaw.Password);
                    authDataRaw.Password = string.Empty;

                    err = ServiceHandler.Login(client, authDataSecure);
                    switch (err)
                    {
                        case 0:
                            Writer.SendTo(client, new Message<string>(Service.Login, Messages.LoginSuccess));
                            break;
                        case ErrorCodes.InvalidCredentialsError:
                            Writer.SendTo(client, new Message<string>(Service.Login, Messages.InvalidCredentials));
                            break;
                        case ErrorCodes.AlreadyLoggedIn:
                            Writer.SendTo(
                                client, new Message<string>(Service.Login, Messages.PlayerAlreadyLoggedIn));
                            break;
                        default:
                            Writer.SendToThenDropConnection(
                                client, new Message<string>(Service.None, Messages.InternalErrorDrop));
                            return;
                    }

                    break;

                case Service.Logout:
                    err = ServiceHandler.Logout(client);
                    switch (err)
                    {
                        case 0:
                            Writer.SendToThenDropConnection(client,
                                new Message<string>(Service.Logout, Messages.LogoutSuccess));
                            break;
                        case ErrorCodes.LogoutError:
                            Writer.SendTo(client, new Message<string>
                                (Service.Logout, Messages.DataNotSaved));
                            break;
                        default:
                            Writer.SendToThenDropConnection(
                                client, new Message<string>
                                (Service.Logout, Messages.InternalErrorDrop));
                            return;
                    }

                    break;

                case Service.Registration:
                    AuthDataRaw authDataInsecure = ((Message<AuthDataRaw>)message).Data;
                    AuthDataSecure authDataReg = new AuthDataSecure(authDataInsecure.Username, authDataInsecure.Password);
                    authDataInsecure.Password = string.Empty;

                    err = ServiceHandler.Register(client, authDataReg);
                    switch (err)
                    {
                        case 0:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.RegisterSuccessful));
                            break;
                        case ErrorCodes.AlreadyLoggedIn:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.AlreadyLoggedIn));
                            break;
                        case ErrorCodes.UsernameEmptyError:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.EmptyUsername));
                            break;
                        case ErrorCodes.PasswordEmptyError:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.EmptyPassword));
                            break;
                        case ErrorCodes.UsernameTakenError:
                            Writer.SendTo(
                            client,
                            new Message<string>(Service.Registration, Messages.UsernameTaken));
                            break;
                        default:
                            Writer.SendToThenDropConnection(
                            client,
                            new Message<string>(Service.Registration, Messages.InternalErrorDrop));
                            return;
                    }

                    break;
            }
        }
    }
}