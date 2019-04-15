using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SharePoint.Authentication.Exceptions;

namespace SharePoint.Authentication.Owin.Models
{
    public class HighTrustCredentials
    {
        public string SharePointHostWebUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public static HighTrustCredentials GenerateRandomHighTrustCredentials(string spHostWebUrl)
        {
            var clientId = Guid.NewGuid().ToString("D");
            var clientSecret = GetSha256(Guid.NewGuid().ToString("D"));

            var credentials = new HighTrustCredentials()
            {
                SharePointHostWebUrl = spHostWebUrl,
                ClientId = clientId,
                ClientSecret = clientSecret,
            };

            return credentials;
        }

        public static HighTrustCredentials GenerateUniqueHighTrustCredentials(string spHostWebUrl, string clientIdSalt, string clientSecretSalt)
        {
            if (string.IsNullOrWhiteSpace(clientIdSalt) || string.IsNullOrWhiteSpace(clientSecretSalt) || clientIdSalt.Equals(clientSecretSalt, StringComparison.InvariantCultureIgnoreCase))
                throw new SharePointAuthenticationException($"Invalid {nameof(clientIdSalt)} or {nameof(clientSecretSalt)}");

            var clientId = new Guid(GetMD5Bytes(GetSha512Bytes(spHostWebUrl))).ToString("D");
            var clientSecret = GetSha256(spHostWebUrl + clientSecretSalt);

            var credentials = new HighTrustCredentials()
            {
                SharePointHostWebUrl = spHostWebUrl,
                ClientId = clientId,
                ClientSecret = clientSecret,
            };

            return credentials;
        }

        private static string GetSha256(string str)
        {
            var crypto = GetSha256Bytes(str);
            return Convert.ToBase64String(crypto);
        }

        private static byte[] GetSha256Bytes(string str)
        {
            using var crypt = new SHA256Managed();
            var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(str));
            return crypto;
        }

        private static byte[] GetSha512Bytes(string str)
        {
            using var crypt = new SHA512Managed();
            var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(str));
            return crypto;
        }

        // ReSharper disable once InconsistentNaming
        private static byte[] GetMD5Bytes(byte[] bytes)
        {
            using var crypt = new MD5Cng();
            var crypto = crypt.ComputeHash(bytes);
            return crypto;
        }
    }
}
