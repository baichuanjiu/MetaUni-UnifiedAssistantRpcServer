using Message.RPC.Entities.Message.Chat;
using Message.RPC.Entities.Message.Message;
using Message.RPC.Entities.Message.SystemPromotion;
using Microsoft.EntityFrameworkCore;

namespace Message.RPC.DataContext.Message
{
    public class MessageContext : DbContext
    {
        public MessageContext(DbContextOptions<MessageContext> options) : base(options)
        {
        }

        public DbSet<Chat> Chats { get; set; }
        public DbSet<SystemPromotion> SystemPromotions { get; set; }
        public DbSet<SystemMessage> SystemMessages { get; set; }
        public DbSet<SystemMessageInbox> SystemMessageInboxes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Chat>();

            modelBuilder.HasSequence<int>("CurrentUUID").StartsAt(10000);
            modelBuilder.Entity<SystemPromotion>().Property(u => u.UUID).HasDefaultValueSql("NEXT VALUE FOR CurrentUUID");
            modelBuilder.Entity<SystemMessage>();
            modelBuilder.Entity<SystemMessageInbox>();
        }
    }
}
