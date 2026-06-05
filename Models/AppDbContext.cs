using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace DocumentUpload_App.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("name=MyDB")
        {
            
        }

        public DbSet<UserLogin> UserLogins { get; set; }

        public DbSet<Document> Documents { get; set; }
        public DbSet<Category> Categories { get; set; }
    }
}