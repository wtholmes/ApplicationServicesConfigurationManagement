namespace PowerShellRunspaceManager
{
    using System.Data.Entity;

    public partial class PowerShellRunspacesManagerDataModel : DbContext
    {
        public PowerShellRunspacesManagerDataModel()
            : base("Data Source=.;initial catalog=PowerShellRunspacesManager;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework")
        {
        }

        public virtual DbSet<CommandQueue> CommandQueues { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}