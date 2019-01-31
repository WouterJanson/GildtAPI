using System;
using System.Security.Cryptography;


public static class PasswordHasher
    {
        private const int HashSize = 20;
        private const int SaltSize = 16;
        private const int HashBytesSize = SaltSize + HashSize;
        private const int NumberOfIterations = 2000; 
        public static string HashPassword(string password)
        {
            if(string.IsNullOrEmpty(password))
            {
                return null; 
            }

            byte[] salt = new byte[SaltSize];
            new RNGCryptoServiceProvider().GetBytes(salt);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, NumberOfIterations);
            byte[] hash = pbkdf2.GetBytes(HashSize);
            byte[] hashBytes = new byte[HashBytesSize];

            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, 16, HashSize);

            string hashedPassword = Convert.ToBase64String(hashBytes);
            return hashedPassword; 
        }

        // Test if the passworh hash is correctly
        public static bool VerifyHashedPassword(string hashedPassword, string unhashedPassword)
        {
            if(string.IsNullOrEmpty(hashedPassword))
            {
                return false; 
            }
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);
            var pbkdf2 = new Rfc2898DeriveBytes(unhashedPassword, salt, NumberOfIterations);
            byte[] hash = pbkdf2.GetBytes(HashSize);

            for (int i = 0; i < HashSize; i++)
            {
                if(hashBytes[i+SaltSize] != hash[i])
                {
                    return false; 
                }
            }

            return true; 
        }
    }