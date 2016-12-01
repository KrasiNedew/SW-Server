namespace Server.Services
{
    using System;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.CommHandlers;
    using Server.Constants;

    using ServerUtils.Wrappers;

    public static class Responses
    {
        public static void InternalError(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendToThenDropConnection(client, Messages.InternalError);
        }

        public static void InvalidCredentials(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.InvalidCredentials);
        }

        public static void Blocked(this AsynchronousSocketListener server, Client client, DateTime timeOfBlock)
        {
            server.Writer.SendTo(client, Messages.Error);
            TimeSpan diff = new TimeSpan(DateTime.Now.Ticks - timeOfBlock.Ticks);
            Message<string> message =
                new Message<string>(Service.Info, $"You are blocked. Try again in {diff.Minutes} min : {diff.Seconds} sec");
            server.Writer.SendToThenDropConnection(client, message);
        }

        public static void ServerFull(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendToThenDropConnection(client, Messages.ConnectionLimitReached);
        }

        public static void MustBeLoggedIn(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.MustLoginFirst);
        }

        public static void AlreadyLoggedIn(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.AlreadyLoggedIn);
        }

        public static void PlayerAlreadyLoggedIn(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.PlayerAlreadyLoggedIn);
        }

        public static void UsernameEmpty(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.EmptyUsername);
        }

        public static void PasswordEmpty(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.EmptyPassword);
        }

        public static void LoginSuccess(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Success);
            server.Writer.SendTo(client, Messages.LoginSuccessful);
        }

        public static void RegisterSuccess(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Success);
            server.Writer.SendTo(client, Messages.RegisterSuccessful);
        }

        public static void LogoutSuccess(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Success);
            server.Writer.SendTo(client, Messages.LogoutSuccess);
        }

        public static void UsernameTaken(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.UsernameTaken);
        }

        public static void SomethingWentWrong(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.SomethingWentWrong);
        }

        public static void ServiceNotRecognized(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendToThenDropConnection(client, Messages.ServiceNotRecognized);
        }

        public static void DataNotSaved(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.DataNotSaved);
        }

        public static void DataSaved(this AsynchronousSocketListener server, Client client)
        {
            server.Writer.SendTo(client, Messages.Success);
            server.Writer.SendTo(client, Messages.DataSaved);
        }
    }
}