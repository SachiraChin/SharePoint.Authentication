using System.Collections.Generic;

namespace SharePoint.Authentication.Owin.Models
{
    internal class AADPublicKey
    {
        public string kty { get; set; }
        public string use { get; set; }
        public string kid { get; set; }
        public string x5t { get; set; }
        public string n { get; set; }
        public string e { get; set; }
        public List<string> x5c { get; set; }
    }
}