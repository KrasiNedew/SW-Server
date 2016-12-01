namespace ModelDTOs
{
    using System;

    using ProtoBuf;

    using ServerUtils;

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
            unchecked
            {
                int result = 37; // prime

                result *= 397; // also prime
                if (this.Username != null)
                {
                    result += this.Username.GetHashCode();
                }

                result *= 397;
                if (this.PasswordHash != null)
                {
                    result += this.PasswordHash.GetHashCode();
                    result *= 397;
                    result += this.PasswordHash.GetHashCode();
                }
                else
                {
                    result *= 397;
                }

                return result;
            }
        }
    }
}