using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Added for ILogger
using ShoppingListApp.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingListApp.Client.Core.Data
{
    public class ShoppingRepository
    {
        private readonly LocalDbContext _dbContext;
        private readonly ILogger<ShoppingRepository> _logger;

        public ShoppingRepository(LocalDbContext dbContext, ILogger<ShoppingRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
             try
            {
                _dbContext.Database.EnsureCreated(); // Ensure DB is created
                _logger.LogInformation("LocalDatabase ensured to be created at path: {Path}", _dbContext.Database.GetDbConnection().DataSource);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring local database is created.");
            }
        }

        // ListItem CRUD
        public async Task<List<ListItem>> GetAllListItemsAsync()
        {
            _logger.LogInformation("Fetching all list items from local DB.");
            return await _dbContext.ListItems
                                 .Include(li => li.Category) // Example of including related data
                                 .Include(li => li.Store)
                                 .Include(li => li.UserList)
                                 .ToListAsync();
        }

        public async Task<List<ListItem>> GetActiveListItemsAsync()
        {
            _logger.LogInformation("Fetching active list items from local DB.");
            return await _dbContext.ListItems
                                 .Where(li => li.IsActive && !li.IsArchived)
                                 .Include(li => li.Category)
                                 .Include(li => li.Store)
                                 .Include(li => li.UserList)
                                 .ToListAsync();
        }


        public async Task<ListItem?> GetListItemByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching list item by ID: {Id}", id);
            return await _dbContext.ListItems
                                 .Include(li => li.Category)
                                 .Include(li => li.Store)
                                 .Include(li => li.UserList)
                                 .FirstOrDefaultAsync(li => li.Id == id);
        }

        public async Task AddListItemAsync(ListItem item)
        {
            _logger.LogInformation("Adding new list item: {ItemName}", item.Name);
            // Ensure related entities are tracked or attached if they already exist.
            // This is important if Category, Store, UserList are passed as detached objects.
            if (item.Category != null) _dbContext.Entry(item.Category).State = EntityState.Unchanged;
            if (item.Store != null) _dbContext.Entry(item.Store).State = EntityState.Unchanged;
            if (item.UserList != null) _dbContext.Entry(item.UserList).State = EntityState.Unchanged;

            _dbContext.ListItems.Add(item);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("List item {ItemName} added successfully.", item.Name);
        }

        public async Task UpdateListItemAsync(ListItem item)
        {
            _logger.LogInformation("Updating list item: {ItemId}", item.Id);
            var existingItem = await _dbContext.ListItems.FindAsync(item.Id);
            if (existingItem != null)
            {
                 // Detach the existing entity before attaching the new one if it's a different instance
                _dbContext.Entry(existingItem).State = EntityState.Detached;
            }
             _dbContext.Entry(item).State = EntityState.Modified;

            // If Category, Store, UserList objects are part of the item and might be new or existing,
            // you might need to manage their state explicitly too.
            // For example, if item.Category is an existing one but its properties are not meant to be updated by this call:
            if (item.Category != null) _dbContext.Entry(item.Category).State = EntityState.Unchanged;
            if (item.Store != null) _dbContext.Entry(item.Store).State = EntityState.Unchanged;
            if (item.UserList != null) _dbContext.Entry(item.UserList).State = EntityState.Unchanged;

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("List item {ItemId} updated successfully.", item.Id);
        }

        public async Task UpsertListItemsAsync(IEnumerable<ListItem> items)
        {
            _logger.LogInformation("Upserting {ItemCount} list items.", items.Count());
            foreach (var item in items)
            {
                var existing = await _dbContext.ListItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.Id);
                if (existing != null)
                {
                    _dbContext.ListItems.Update(item);
                }
                else
                {
                    _dbContext.ListItems.Add(item);
                }
            }
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Upsert operation completed.");
        }


        public async Task DeleteListItemAsync(Guid id)
        {
            _logger.LogInformation("Deleting list item by ID: {Id}", id);
            var item = await _dbContext.ListItems.FindAsync(id);
            if (item != null)
            {
                _dbContext.ListItems.Remove(item);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("List item {Id} deleted successfully.", id);
            }
            else
            {
                _logger.LogWarning("List item {Id} not found for deletion.", id);
            }
        }

        public async Task DeleteListItemsAsync(IEnumerable<Guid> ids)
        {
            _logger.LogInformation("Deleting multiple list items. Count: {Count}", ids.Count());
            var itemsToDelete = await _dbContext.ListItems.Where(li => ids.Contains(li.Id)).ToListAsync();
            if (itemsToDelete.Any())
            {
                _dbContext.ListItems.RemoveRange(itemsToDelete);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("{Count} list items deleted successfully.", itemsToDelete.Count);
            }
            else
            {
                _logger.LogInformation("No list items found for the provided IDs to delete.");
            }
        }


        // Category CRUD (Simplified - assuming categories are mostly static or managed by sync)
        public async Task<List<Category>> GetAllCategoriesAsync() => await _dbContext.Categories.ToListAsync();
        public async Task UpsertCategoriesAsync(IEnumerable<Category> categories)
        {
             _logger.LogInformation("Upserting {CategoriesCount} categories.", categories.Count());
            foreach (var category in categories)
            {
                var existing = await _dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == category.Id);
                if (existing != null) _dbContext.Categories.Update(category); else _dbContext.Categories.Add(category);
            }
            await _dbContext.SaveChangesAsync();
        }


        // Store CRUD (Simplified)
        public async Task<List<Store>> GetAllStoresAsync() => await _dbContext.Stores.ToListAsync();
        public async Task UpsertStoresAsync(IEnumerable<Store> stores)
        {
            _logger.LogInformation("Upserting {StoresCount} stores.", stores.Count());
            foreach (var store in stores)
            {
                var existing = await _dbContext.Stores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == store.Id);
                if (existing != null) _dbContext.Stores.Update(store); else _dbContext.Stores.Add(store);
            }
            await _dbContext.SaveChangesAsync();
        }

        // UserList CRUD (Simplified)
        public async Task<List<UserList>> GetAllUserListsAsync() => await _dbContext.UserLists.ToListAsync();
         public async Task UpsertUserListsAsync(IEnumerable<UserList> userLists)
        {
            _logger.LogInformation("Upserting {UserListsCount} user lists.", userLists.Count());
            foreach (var list in userLists)
            {
                var existing = await _dbContext.UserLists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == list.Id);
                if (existing != null) _dbContext.UserLists.Update(list); else _dbContext.UserLists.Add(list);
            }
            await _dbContext.SaveChangesAsync();
        }

        // Get all data for sync (could be more granular in a real app)
        public async Task<SyncRequestDto> GetLocalChangesForSyncAsync(DateTime lastSyncTimestamp)
        {
            _logger.LogInformation("Getting local changes for sync since: {LastSyncTimestamp}", lastSyncTimestamp);
            // This is a simplified version. A real app would track changes (e.g., using a "ModifiedDate" or a separate changes table).
            // For now, sending all local items as "updated" if they are newer than last sync, or simply all items.
            // Let's assume for now we send all current items as potentially updated.
            // Deletions need a soft-delete mechanism or a separate log of deleted IDs.
            // For this example, we'll assume deletions are handled by client logic before calling this,
            // or that SyncRequestDto is populated elsewhere with deleted IDs.

            // This example doesn't implement true change tracking for отправки.
            // It would require 'IsDirty' flags, 'LastModified' timestamps on entities, or a separate change log.
            // For now, we'll just return all current items. The SyncService will need to manage this.
            var allItems = await _dbContext.ListItems.ToListAsync();

            return new SyncRequestDto
            {
                UpdatedItems = allItems, // Simplification: sending all items
                DeletedItemIds = new List<Guid>(), // Needs proper tracking
                LastSyncTimestamp = lastSyncTimestamp // Client should manage and pass its actual last sync time
            };
        }
         public async Task<DateTime> GetLastSyncTimestampAsync()
        {
            // Placeholder: In a real app, this would be stored persistently.
            // For now, return a very old date to ensure first sync gets everything.
            // Or, it could be stored in a simple key-value table within SQLite.
            var syncState = await _dbContext.KeyValueStates.FirstOrDefaultAsync(s => s.Key == "LastSyncTimestamp");
            if (syncState != null && DateTime.TryParse(syncState.Value, out DateTime lastSync))
            {
                return lastSync;
            }
            return DateTime.MinValue; // Default if not found
        }

        public async Task SetLastSyncTimestampAsync(DateTime timestamp)
        {
            var syncState = await _dbContext.KeyValueStates.FirstOrDefaultAsync(s => s.Key == "LastSyncTimestamp");
            if (syncState == null)
            {
                syncState = new KeyValueState { Key = "LastSyncTimestamp" };
                _dbContext.KeyValueStates.Add(syncState);
            }
            syncState.Value = timestamp.ToString("o"); // ISO 8601 format
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Last sync timestamp updated to: {Timestamp}", timestamp);
        }
    }

    // Simple class for storing key-value pairs like last sync time
    public class KeyValueState
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
