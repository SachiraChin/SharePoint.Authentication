using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace SharePoint.Authentication.Sample.DataContext
{
    public class SampleDataContext : DbContext
    {
        public SampleDataContext() : base("SampleConnectionString")
        {
        }

        public DbSet<SampleSharePointSession> SampleSharePointSessions { get; set; }
    }
}