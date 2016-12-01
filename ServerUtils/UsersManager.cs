namespace ServerUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Data;

    using ModelDTOs;

    public class UsersManager
    {
        private readonly Dictionary<UserFull, UserFull> users;

        public UsersManager()
        {
            this.users = new Dictionary<UserFull, UserFull>();
            this.LoadUsers();
        }

        public bool Exists(UserFull user)
        {
            return user != null && this.users.ContainsKey(user);
        }

        public bool IsLoggedIn(UserFull user)
        {
            return this.Exists(user) && this.users[user].LoggedIn;
        }

        public void MarkRegister(UserFull user)
        {
            if(!this.IsValidCleanUser(user))
                throw new ArgumentException("User data is invalid");

            this.users.Add(user, user);
        }

        public void MarkLogout(UserFull user)
        {
            if (!this.Exists(user)) return;

            this.users[user].LoggedIn = false;
        }

        public void MarkLogin(UserFull user)
        {
            if (!this.Exists(user)) return;

            this.users[user].LoggedIn = true;
        }

        public int GetId(UserFull user)
        {
            if (!this.Exists(user)) return -1;

            return this.users[user].Id;
        }

        public UserFull GetUser(UserFull user)
        {
            if (!this.Exists(user)) return null;

            return this.users[user];
        }

        public bool IsValidOnlineUser(UserFull user)
        {
            return user?.Username != null && user.PasswordHash != null && this.IsLoggedIn(user) && user.Id != 0;
        }

        public bool IsValidOfflineUser(UserFull user)
        {
            return user?.Username != null 
                && user.PasswordHash != null 
                && this.Exists(user);
        }

        public bool IsValidCleanUser(UserFull user)
        {
            return user?.Username != null && user.PasswordHash != null;
        }

        public IEnumerable<UserFull> GetAll()
        {
            return this.users.Values;
        }

        private void LoadUsers()
        {
            using (SimpleWarsContext context = new SimpleWarsContext())
            {
                var data = context.Players.Select(p => new { p.Username, p.PasswordHash, p.Id, p.LoggedIn });
                
                foreach (var entry in data)
                {
                    var user = new UserFull(entry.Username, entry.PasswordHash, entry.Id, entry.LoggedIn);

                    this.users.Add(user, user);
                }
            }
        }
    }
}