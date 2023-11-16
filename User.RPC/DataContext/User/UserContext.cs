using Microsoft.EntityFrameworkCore;
using User.RPC.Entities.User;

namespace User.RPC.DataContext.User
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProfile>();
        }

    }
}
