namespace Server.Constants
{
    public static class MessageText
    {
        public const string InternalErrorDrop = "Internal Error. Dropping you connection";

        // #besterrorresponse
        public const string SomethingWentWrong = "Oops something went wrong";

        public const string LoginSuccess = "Logged in successfuly";

        public const string InvalidCredentials = "Invalid credentials";

        public const string AlreadyLoggedIn = "You are already logged in";

        public const string PlayerAlreadyLoggedIn = "Player is already logged in.";

        public const string EmptyUsername = "Username cannot be empty";

        public const string EmptyPassword = "Password cannot be empty";

        public const string UsernameTaken = "Username already taken";

        public const string RegisterSuccessful = "Registered successfuly";

        public const string LogoutSuccess = "Logged out successfuly";

        public const string DataNotSaved = "An error occured while saving your data.";

        public const string DataSaved = "Your data was saved";

        public const string ConnectionLimitReached = "Server is full. Try again later later.";

        public const string ServiceNotRecognized = "The requested service was not recognized. Your client is most likely corrupt/modified and thus your connection will be dropped.";

        public const string MustBeLoggedIn = "You must log in first.";
    }
}