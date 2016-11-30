namespace ModelDTOs
{
    using System;

    using ProtoBuf;

    [ProtoContract]
    public class UserFull : IEquatable<UserFull>
    {
        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public string PasswordHash { get; set; }

        public int Id { get; set; }

        [ProtoMember(3)]
        public bool LoggedIn { get; set; }

        protected UserFull()
        {
        }

        public UserFull(string username, string passwordHash, int id, bool loggedIn)
        {
            this.Username = username;
            this.PasswordHash = passwordHash;
            this.Id = id;
            this.LoggedIn = loggedIn;
        }

        public bool Equals(UserFull other)
        {
            return this.Username == other?.Username && this.PasswordHash == other?.PasswordHash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as UserFull;
            if (other == null) return false;

            return this.Username == other.Username && this.PasswordHash == other.PasswordHash;
        }

        public override int GetHashCode()
        {
            return string.Concat(this.Username, this.PasswordHash).GetHashCode();
        }
    }
}