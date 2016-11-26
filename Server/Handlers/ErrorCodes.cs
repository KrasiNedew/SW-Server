namespace Server.Handlers
{
    public static class ErrorCodes
    {
        public const int InternalError = 1;

        public const int InvalidCredentialsError = 2;

        public const int LogoutError = 3;

        public const int UsernameTakenError = 4;

        public const int PasswordEmptyError = 5;

        public const int UsernameEmptyError = 6;

        public const int AlreadyLoggedIn = 7;
    }
}