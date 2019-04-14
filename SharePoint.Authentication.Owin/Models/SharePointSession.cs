using System;

namespace SharePoint.Authentication.Owin.Models
{
    public class SharePointSession
    {
        public Guid SessionId { get; set; }
        public string? ContextToken { get; set; }
        public string ContextTokenAuthority { get; set; }
        public string SharePointHostWebUrl { get; set; }
        public string SharePointAppWebUrl { get; set; }
    }
}
