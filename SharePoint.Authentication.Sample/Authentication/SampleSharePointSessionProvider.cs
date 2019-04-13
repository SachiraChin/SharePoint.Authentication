using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SharePoint.Authentication.Sample.Authentication
{
    public class SampleSharePointSessionProvider : ISharePointSessionProvider
    {
        public Task SaveSharePointSession(Guid sessionId, SharePointSession sharePointSession)
        {
            throw new NotImplementedException();
        }

        public Task UpdateSharePointSession(Guid sessionId, SharePointSession sharePointSession)
        {
            throw new NotImplementedException();
        }

        public Task<SharePointSession> GetSharePointSession(Guid sessionId)
        {
            throw new NotImplementedException();
        }
    }
}