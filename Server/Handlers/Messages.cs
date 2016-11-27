namespace Server.Handlers
{
    public static class Messages
    {
        public const string InternalErrorDrop = "Internal Error. Dropping you connection";

        // #besterrorresponse
        public const string SomethingWentWrong = "Oops something went wrong";

        public const string LoginSuccess = "Logged in successfuly";

        public const string InvalidCredentials = "Invalid credentials";

        public const string AlreadyLoggedIn = "You are already logged in";

        public const string EmptyUsername = "Username cannot be empty";

        public const string EmptyPassword = "Password cannot be empty";

        public const string UsernameTaken = "Username already taken";

        public const string RegisterSuccessful = "Registered successfuly";

        public const string LogoutSuccess = "Logged out successfuly";

        public const string DataNotSaved = "An error occured while saving your data.";
    }
}