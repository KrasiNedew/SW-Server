namespace ModelDTOs
{
    using ProtoBuf;

    [ProtoContract]
    public class AuthDTO
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        protected AuthDTO()
        {
        }

        public AuthDTO(string username, string passwordHash)
        {
            this.Username = username;
            this.PasswordHash = passwordHash;
        }
    }
}