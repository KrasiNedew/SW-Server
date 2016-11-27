namespace ModelDTOs
{
    using ProtoBuf;

    [ProtoContract]
    public class AuthDataRawDTO
    {
        protected AuthDataRawDTO()
        {
        }

        public AuthDataRawDTO(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public string Password { get; set; }
    }
}