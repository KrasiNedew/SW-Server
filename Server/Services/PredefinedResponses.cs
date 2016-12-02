namespace Server.Services
{
    using System;

    using ModelDTOs;
    using ModelDTOs.Enums;

    using Server.Constants;

    using ServerUtils.Wrappers;

    public class PredefinedResponses
    {
        public readonly AsynchronousSocketListener server;

        public PredefinedResponses(AsynchronousSocketListener server)
        {
            this.server = server;
        }

        public void Success(Client client)
        {
            this.server.Writer.SendTo(client, Messages.Success);
        }

        public void Error(Client client)
        {
            this.server.Writer.SendTo(client, Messages.Error);
        }

        public void InternalError(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendToThenDropConnection(client, Messages.InternalError);
        }

        public void InvalidCredentials(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.InvalidCredentials);
        }

        public void Blocked(Client client, DateTime timeOfBlock)
        {
            server.Writer.SendTo(client, Messages.Error);
            TimeSpan diff = new TimeSpan(DateTime.Now.Ticks - timeOfBlock.Ticks);
            Message<string> message =
                new Message<string>(Service.Info, $"You are blocked. Try again in {diff.Minutes} min : {diff.Seconds} sec");
            server.Writer.SendToThenDropConnection(client, message);
        }

        public void ServerFull(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendToThenDropConnection(client, Messages.ConnectionLimitReached);
        }

        public void MustBeLoggedIn(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.MustLoginFirst);
        }

        public void AlreadyLoggedIn(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.AlreadyLoggedIn);
        }

        public void PlayerAlreadyLoggedIn(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.PlayerAlreadyLoggedIn);
        }

        public void UsernameEmpty(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.EmptyUsername);
        }

        public void PasswordEmpty(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.EmptyPassword);
        }

        public void LoginSuccess(Client client)
        {
            server.Writer.SendTo(client, Messages.Success);
            server.Writer.SendTo(client, Messages.LoginSuccessful);
        }

        public void RegisterSuccess(Client client)
        {
            server.Writer.SendTo(client, Messages.Success);
            server.Writer.SendTo(client, Messages.RegisterSuccessful);
        }

        public void LogoutSuccess(Client client)
        {
            server.Writer.SendTo(client, Messages.Success);
            server.Writer.SendTo(client, Messages.LogoutSuccess);
        }

        public void UsernameTaken(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.UsernameTaken);
        }

        public void SomethingWentWrong(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.SomethingWentWrong);
        }

        public void ServiceNotRecognized(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendToThenDropConnection(client, Messages.ServiceNotRecognized);
        }

        public void DataNotSaved(Client client)
        {
            server.Writer.SendTo(client, Messages.Error);
            server.Writer.SendTo(client, Messages.DataNotSaved);
        }

        public void DataSaved(Client client)
        {
            server.Writer.SendTo(client, Messages.Success);
            server.Writer.SendTo(client, Messages.DataSaved);
        }
    }
}