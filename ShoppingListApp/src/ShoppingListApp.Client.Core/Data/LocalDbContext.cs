using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Shared.Models;

namespace ShoppingListApp.Client.Core.Data
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
        {
        }

        public DbSet<ListItem> ListItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<UserList> UserLists { get; set; }
        public DbSet<KeyValueState> KeyValueStates { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<KeyValueState>()
                .HasKey(kvs => kvs.Key); // Define primary key for KeyValueState

            // Seed initial data if needed, or ensure this is handled by first sync
            var foodCategoryId = Guid.NewGuid();
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = foodCategoryId, Name = "Groceries (Local)" }
            );

            var localStoreId = Guid.NewGuid();
            modelBuilder.Entity<Store>().HasData(
                new Store { Id = localStoreId, Name = "My Local Market (Local)" }
            );

            var defaultUserListId = Guid.NewGuid();
            modelBuilder.Entity<UserList>().HasData(
                new UserList { Id = defaultUserListId, Name = "My Shopping List (Local)"}
            );

            modelBuilder.Entity<ListItem>().HasData(
                new ListItem {
                    Id = Guid.NewGuid(),
                    Name = "Apples (Local)",
                    CategoryId = foodCategoryId,
                    StoreId = localStoreId,
                    PurchaseType = PurchaseType.Offline,
                    IsRecurring = false,
                    IsActive = true,
                    IsArchived = false,
                    UserListId = defaultUserListId
                }
            );
        }
    }
}
