using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SharePoint.Authentication
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
