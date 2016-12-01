namespace ModelDTOs
{
    using System;

    using ProtoBuf;

    [ProtoContract]
    public class UserLimited : IEquatable<UserLimited>
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

        public static UserLimited Create(string username, bool loggedIn)
        {
            return new UserLimited(username, loggedIn);
        }

        public bool Equals(UserLimited other)
        {
            if (other == null) return false;

            return this.Username == other.Username;
        }

        public override bool Equals(object obj)
        {
            var other = obj as UserLimited;

            if (other == null) return false;

            return this.Username == other.Username;
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
                result *= 397;

                return result;
            }
        }
    }
}