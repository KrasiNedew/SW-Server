namespace ModelDTOs
{
    using ProtoBuf;

    [ProtoContract]
    public class UserLimited
    {
        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public bool LoggedIn { get; set; }

        protected UserLimited()
        {
        }

        public UserLimited(string username, bool loggedIn)
        {
            this.Username = username;
            this.LoggedIn = loggedIn;
        }
    }
}