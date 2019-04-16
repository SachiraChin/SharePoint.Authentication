using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SharePoint.Authentication.Sample.DataContext
{
    public class SampleHighTrustCredentials
    {
        public int Id { get; set; }
        [Index("IDX_SharePointHostWebUrl", IsClustered = false, IsUnique = true)]
        [MaxLength(64)]
        public string SharePointHostWebUrlHash { get; set; }
        public string SharePointHostWebUrl { get; set; }
        public byte[] ClientId { get; set; }
        public byte[] ClientSecret { get; set; }
    }
}