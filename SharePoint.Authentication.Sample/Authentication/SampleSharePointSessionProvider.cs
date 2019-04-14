using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SharePoint.Authentication.Owin;
using SharePoint.Authentication.Owin.Models;
using SharePoint.Authentication.Sample.DataContext;

namespace SharePoint.Authentication.Sample.Authentication
{
    public class SampleSharePointSessionProvider : ISharePointSessionProvider
    {
        private const string VerySecurePassword =
            "-xSy>4]W2dQS)7+6rQ<+T'~wU}((BwP#h\"LmL@TEe7skPX3EE}g-E/JV+:+X\"K.P&ZF3\\SnV#AS^=G.@{,NEUE^mB2xgP<$9sHy^`_p{uaAt,2Y#.5kJu\"4'WmEue;M,";

        public async Task SaveSharePointSession(Guid sessionId, SharePointSession sharePointSession)
        {
            using var context = new SampleDataContext();
            var model = new SampleSharePointSession()
            {
                SessionId = sessionId,
                ContextToken = string.IsNullOrWhiteSpace(sharePointSession.ContextToken) ? null : StringCipher.Encrypt(sharePointSession.ContextToken, VerySecurePassword),
                ContextTokenAuthority = sharePointSession.ContextTokenAuthority,
                SharePointAppWebUrl = sharePointSession.SharePointAppWebUrl,
                SharePointHostWebUrl = sharePointSession.SharePointHostWebUrl,
            };
            context.SampleSharePointSessions.Add(model);
            await context.SaveChangesAsync();
        }

        public async Task UpdateSharePointSession(Guid sessionId, SharePointSession sharePointSession)
        {
            using var context = new SampleDataContext();
            var model = new SampleSharePointSession()
            {
                SessionId = sessionId,
                ContextToken = string.IsNullOrWhiteSpace(sharePointSession.ContextToken) ? null : StringCipher.Encrypt(sharePointSession.ContextToken, VerySecurePassword),
                ContextTokenAuthority = sharePointSession.ContextTokenAuthority,
                SharePointAppWebUrl = sharePointSession.SharePointAppWebUrl,
                SharePointHostWebUrl = sharePointSession.SharePointHostWebUrl,
            };
            context.Entry(model).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        public async Task<SharePointSession> GetSharePointSession(Guid sessionId)
        {
            using var context = new SampleDataContext();
            var dbModel = await context.SampleSharePointSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (dbModel == null) return null;
            var model = new SharePointSession()
            {
                SessionId = sessionId,
                ContextToken = dbModel.ContextToken == null ? null : StringCipher.Decrypt(dbModel.ContextToken, VerySecurePassword),
                ContextTokenAuthority = dbModel.ContextTokenAuthority,
                SharePointAppWebUrl = dbModel.SharePointAppWebUrl,
                SharePointHostWebUrl = dbModel.SharePointHostWebUrl,
            };

            return model;
        }

        public async Task SaveHighTrustCredentials(HighTrustCredentials highTrustCredentials)
        {
            using var context = new SampleDataContext();
            var model = new SampleHighTrustCredentials()
            {
                ClientId = string.IsNullOrWhiteSpace(highTrustCredentials.ClientId) ? null : StringCipher.Encrypt(highTrustCredentials.ClientId, VerySecurePassword),
                ClientSecret = string.IsNullOrWhiteSpace(highTrustCredentials.ClientSecret) ? null : StringCipher.Encrypt(highTrustCredentials.ClientSecret, VerySecurePassword),
                SharePointHostWebUrl = highTrustCredentials.SharePointHostWebUrl,
                SharePointHostWebUrlHash = GetSha256(highTrustCredentials.SharePointHostWebUrl),
            };

            context.SampleHighTrustCredentials.Add(model);
            await context.SaveChangesAsync();
        }

        public async Task<HighTrustCredentials> GetHighTrustCredentials(string spHostWebUrl)
        {
            using var context = new SampleDataContext();
            var spHostWebUrlHash = GetSha256(spHostWebUrl);
            var dbModel = await context.SampleHighTrustCredentials.FirstOrDefaultAsync(c => c.SharePointHostWebUrlHash == spHostWebUrlHash);

            return new HighTrustCredentials()
            {
                ClientId = dbModel.ClientId == null ? null : StringCipher.Decrypt(dbModel.ClientId, VerySecurePassword),
                ClientSecret = dbModel.ClientSecret == null ? null : StringCipher.Decrypt(dbModel.ClientSecret, VerySecurePassword),
                SharePointHostWebUrl = dbModel.SharePointHostWebUrl,
            };
        }

        public static string GetSha256(string str)
        {
            using var crypt = new SHA256Managed();
            var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(str));
            return Convert.ToBase64String(crypto);
        }
    }
}