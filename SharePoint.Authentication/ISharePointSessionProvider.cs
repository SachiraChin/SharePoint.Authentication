using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePoint.Authentication
{
    public interface ISharePointSessionProvider
    {
        Task SaveSharePointSession(Guid sessionId, SharePointSession sharePointSession);
        Task UpdateSharePointSession(Guid sessionId, SharePointSession sharePointSession);
        Task<SharePointSession> GetSharePointSession(Guid sessionId);
    }
}
