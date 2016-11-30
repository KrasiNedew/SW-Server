namespace Server.Services
{
    using System;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.CommHandlers;
    using Server.Constants;

    using ServerUtils;

    public static class Responses
    {
        public static void InternalError(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendToThenDropConnection(client, Messages.InternalError);
        }

        public static void InvalidCredentials(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendToThenDropConnection(client, Messages.InvalidCredentials);
        }

        public static void Blocked(Client client, DateTime timeOfBlock)
        {
            Writer.SendTo(client, Messages.Error);
            TimeSpan diff = new TimeSpan(DateTime.Now.Ticks - timeOfBlock.Ticks);
            Message<string> message =
                new Message<string>(Service.Info, $"You are blocked. Try again in {diff.Minutes} min : {diff.Seconds} sec");
            Writer.SendToThenDropConnection(client, message);
        }

        public static void ServerFull(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendToThenDropConnection(client, Messages.ConnectionLimitReached);
        }

        public static void MustBeLoggedIn(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.MustLoginFirst);
        }

        public static void AlreadyLoggedIn(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.AlreadyLoggedIn);
        }

        public static void PlayerAlreadyLoggedIn(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.PlayerAlreadyLoggedIn);
        }

        public static void UsernameEmpty(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.EmptyUsername);
        }

        public static void PasswordEmpty(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.EmptyPassword);
        }

        public static void LoginSuccess(Client client)
        {
            Writer.SendTo(client, Messages.Success);
            Writer.SendTo(client, Messages.LoginSuccessful);
        }

        public static void RegisterSuccess(Client client)
        {
            Writer.SendTo(client, Messages.Success);
            Writer.SendTo(client, Messages.RegisterSuccessful);
        }

        public static void LogoutSuccess(Client client)
        {
            Writer.SendTo(client, Messages.Success);
            Writer.SendTo(client, Messages.LogoutSuccess);
        }

        public static void UsernameTaken(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.UsernameTaken);
        }

        public static void SomethingWentWrong(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.SomethingWentWrong);
        }

        public static void ServiceNotRecognized(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.ServiceNotRecognized);
        }

        public static void DataNotSaved(Client client)
        {
            Writer.SendTo(client, Messages.Error);
            Writer.SendTo(client, Messages.DataNotSaved);
        }

        public static void DataSaved(Client client)
        {
            Writer.SendTo(client, Messages.Success);
            Writer.SendTo(client, Messages.DataSaved);
        }
    }
}