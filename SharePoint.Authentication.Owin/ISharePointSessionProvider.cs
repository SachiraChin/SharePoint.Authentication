using System;
using System.Threading.Tasks;
using SharePoint.Authentication.Owin.Models;

namespace SharePoint.Authentication.Owin
{
    public interface ISharePointSessionProvider
    {
        Task SaveSharePointSession(Guid sessionId, SharePointSession sharePointSession);
        Task UpdateSharePointSession(Guid sessionId, SharePointSession sharePointSession);
        Task<SharePointSession> GetSharePointSession(Guid sessionId);
    }
}
