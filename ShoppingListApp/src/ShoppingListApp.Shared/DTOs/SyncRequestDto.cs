using System;
using System.Collections.Generic;
using ShoppingListApp.Shared.Models;

namespace ShoppingListApp.Shared.DTOs
{
    public class SyncRequestDto
    {
        public List<ListItem> UpdatedItems { get; set; } = new List<ListItem>();
        public List<Guid> DeletedItemIds { get; set; } = new List<Guid>();
        // Potentially add other lists for categories, stores, userlists if they are client-modifiable
        // For now, focusing on ListItem as per typical sync scenarios.
        // public List<Category> UpdatedCategories { get; set; } = new List<Category>();
        // public List<Guid> DeletedCategoryIds { get; set; } = new List<Guid>();
        // public List<Store> UpdatedStores { get; set; } = new List<Store>();
        // public List<Guid> DeletedStoreIds { get; set; } = new List<Guid>();
        // public List<UserList> UpdatedUserLists { get; set; } = new List<UserList>();
        // public List<Guid> DeletedUserListIds { get; set; } = new List<Guid>();
        public DateTime LastSyncTimestamp { get; set; } // Client's last sync time to help server determine changes
    }
}
