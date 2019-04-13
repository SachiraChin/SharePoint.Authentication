using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SharePoint.Authentication.Sample.DataContext;

namespace SharePoint.Authentication.Sample.Authentication
{
    public class SampleSharePointSessionProvider : ISharePointSessionProvider
    {
        private const string VerySecurePassword =
            "-xSy>4]W2dQS)7+6rQ<+T'~wU}((BwP#h\"LmL@TEe7skPX3EE}g-E/JV+:+X\"K.P&ZF3\\SnV#AS^=G.@{,NEUE^mB2xgP<$9sHy^`_p{uaAt,2Y#.5kJu\"4'WmEue;M,";

        public async Task SaveSharePointSession(Guid sessionId, SharePointSession sharePointSession)
        {
            using (var context = new SampleDataContext())
            {
                var model = new SampleSharePointSession()
                {
                    SessionId = sessionId,
                    ContextToken = StringCipher.Encrypt(sharePointSession.ContextToken, VerySecurePassword),
                    ContextTokenAuthority = sharePointSession.ContextTokenAuthority,
                    SharePointAppWebUrl = sharePointSession.SharePointAppWebUrl,
                    SharePointHostWebUrl = sharePointSession.SharePointHostWebUrl,
                };
                context.SampleSharePointSessions.Add(model);
                await context.SaveChangesAsync();
            }
        }

        public async Task UpdateSharePointSession(Guid sessionId, SharePointSession sharePointSession)
        {
            using (var context = new SampleDataContext())
            {
                var model = new SampleSharePointSession()
                {
                    SessionId = sessionId,
                    ContextToken = StringCipher.Encrypt(sharePointSession.ContextToken, VerySecurePassword),
                    ContextTokenAuthority = sharePointSession.ContextTokenAuthority,
                    SharePointAppWebUrl = sharePointSession.SharePointAppWebUrl,
                    SharePointHostWebUrl = sharePointSession.SharePointHostWebUrl,
                };
                context.Entry(model).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
        }

        public async Task<SharePointSession> GetSharePointSession(Guid sessionId)
        {
            using (var context = new SampleDataContext())
            {
                var dbModel = await context.SampleSharePointSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
                if (dbModel == null) return null;
                var model = new SharePointSession()
                {
                    SessionId = sessionId,
                    ContextToken = StringCipher.Decrypt(dbModel.ContextToken, VerySecurePassword),
                    ContextTokenAuthority = dbModel.ContextTokenAuthority,
                    SharePointAppWebUrl = dbModel.SharePointAppWebUrl,
                    SharePointHostWebUrl = dbModel.SharePointHostWebUrl,
                };

                return model;
            }
        }
    }
}