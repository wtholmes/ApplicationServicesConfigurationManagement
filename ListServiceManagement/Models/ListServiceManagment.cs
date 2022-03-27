using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace ListServiceManagement.Models
{
    public partial class ListServiceManagment : DbContext
    {
        public ListServiceManagment()
            : base("name=ListServiceManagment")
        {
        }

        public virtual DbSet<ElistContact> ElistContacts { get; set; }
        public virtual DbSet<ElistContacts_History> ElistContacts_History { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}
