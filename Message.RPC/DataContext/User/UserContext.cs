using Message.RPC.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Message.RPC.DataContext.User
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<UserSyncTable> UserSyncTables { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<int>("CurrentUUID").StartsAt(10000000);

            modelBuilder.Entity<UserAccount>().Property(u => u.UUID).HasDefaultValueSql("NEXT VALUE FOR CurrentUUID");
            modelBuilder.Entity<UserSyncTable>();
        }

    }
}
