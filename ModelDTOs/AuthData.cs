﻿namespace ModelDTOs
{
    using System.Text;

    public class AuthData
    {
        public AuthData(string username, string password)
        {
            this.Username = username;
            this.PasswordHash = GenerateHash(password);
        }

        public string Username { get; set; }

        public string PasswordHash { get; set; }

        private static string GenerateHash(string password)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            System.Text.StringBuilder hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password), 0, Encoding.UTF8.GetByteCount(password));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            return hash.ToString();
        }
    }
}