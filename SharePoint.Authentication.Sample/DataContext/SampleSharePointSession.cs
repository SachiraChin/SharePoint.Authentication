using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SharePoint.Authentication.Sample.DataContext
{
    public class SampleSharePointSession
    {
        [Key]
        public Guid SessionId { get; set; }

        public byte[] ContextToken { get; set; }
        public string ContextTokenAuthority { get; set; }
        public string SharePointHostWebUrl { get; set; }
        public string SharePointAppWebUrl { get; set; }
    }
}