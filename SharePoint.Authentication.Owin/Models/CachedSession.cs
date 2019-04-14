using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Owin.Models
{
    internal class CachedSession
    {
        public string AccessToken { get; set; }
        public string SharePointHostWebUrl { get; set; }
        public string HighTrustClientId { get; set; }
        public string HighTrustClientSecret { get; set; }
    }
}
