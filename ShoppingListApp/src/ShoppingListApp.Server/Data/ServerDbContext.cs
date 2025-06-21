using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Shared.Models;

namespace ShoppingListApp.Server.Data
{
    public class ServerDbContext : DbContext
    {
        public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }

        public DbSet<ListItem> ListItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<UserList> UserLists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Example of configuring a composite key if UserListId and Name for ListItem were unique together
            // modelBuilder.Entity<ListItem>()
            //    .HasKey(li => new { li.UserListId, li.Name }); // Just an example, not in requirements

            // Seed data (optional, but good for development/testing)
            var foodCategoryId = Guid.NewGuid();
            var techCategoryId = Guid.NewGuid();
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = foodCategoryId, Name = "Groceries" },
                new Category { Id = techCategoryId, Name = "Electronics" }
            );

            var localStoreId = Guid.NewGuid();
            var onlineStoreId = Guid.NewGuid();
            modelBuilder.Entity<Store>().HasData(
                new Store { Id = localStoreId, Name = "Local Supermarket" },
                new Store { Id = onlineStoreId, Name = "Online Tech Store" }
            );

            var defaultUserListId = Guid.NewGuid();
            modelBuilder.Entity<UserList>().HasData(
                new UserList { Id = defaultUserListId, Name = "Default Shopping List"}
            );

            // Example seed for ListItem
            modelBuilder.Entity<ListItem>().HasData(
                new ListItem {
                    Id = Guid.NewGuid(),
                    Name = "Milk",
                    CategoryId = foodCategoryId,
                    StoreId = localStoreId,
                    PurchaseType = PurchaseType.Offline,
                    IsRecurring = false,
                    IsActive = true,
                    IsArchived = false,
                    UserListId = defaultUserListId
                },
                new ListItem {
                    Id = Guid.NewGuid(),
                    Name = "Laptop",
                    CategoryId = techCategoryId,
                    StoreId = onlineStoreId,
                    PurchaseType = PurchaseType.Online,
                    IsRecurring = false,
                    IsActive = true,
                    IsArchived = false,
                    UserListId = defaultUserListId
                }
            );
        }
    }
}
