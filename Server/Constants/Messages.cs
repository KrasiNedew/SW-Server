namespace Server.Constants
{
    using ModelDTOs;
    using ModelDTOs.Enums;

    public static class Messages
    {
        public static readonly Message<string> InternalError = 
            Message.Create(MessageText.InternalErrorDrop);

        public static readonly Message<string> InvalidCredentials = 
            Message.Create(MessageText.InvalidCredentials);

        public static readonly Message<string> SomethingWentWrong = 
            Message.Create(MessageText.SomethingWentWrong);

        public static readonly Message<string> AlreadyLoggedIn =
            Message.Create(MessageText.AlreadyLoggedIn);

        public static readonly Message<string> PlayerAlreadyLoggedIn = 
            Message.Create(MessageText.PlayerAlreadyLoggedIn);

        public static readonly Message<string> EmptyUsername = 
            Message.Create(MessageText.EmptyUsername);

        public static readonly Message<string> EmptyPassword = 
            Message.Create(MessageText.EmptyPassword);

        public static readonly Message<string> UsernameTaken = 
            Message.Create(MessageText.UsernameTaken);

        public static readonly Message<string> RegisterSuccessful = 
            Message.Create(MessageText.RegisterSuccessful);

        public static readonly Message<string> LoginSuccessful =
            Message.Create(MessageText.LoginSuccess);

        public static readonly Message<string> LogoutSuccess =
            Message.Create(MessageText.LogoutSuccess);

        public static readonly Message<string> ServiceNotRecognized =
            Message.Create(MessageText.ServiceNotRecognized);

        public static readonly Message<string> ConnectionLimitReached =
            Message.Create(MessageText.ConnectionLimitReached);

        public static readonly Message<string> DataNotSaved =
            Message.Create(MessageText.DataNotSaved);

        public static readonly Message<string> DataSaved =
            Message.Create(MessageText.DataSaved);

        public static readonly Message<string> MustLoginFirst = 
            Message.Create(MessageText.MustBeLoggedIn);

        public static readonly Message<byte> Ping = Message.Create(Service.Ping, (byte)1);
    }
}