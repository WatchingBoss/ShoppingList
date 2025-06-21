using System;
using System.Collections.Generic;
using ShoppingListApp.Shared.Models;

namespace ShoppingListApp.Shared.DTOs
{
    public class SyncResponseDto
    {
        public List<ListItem> ServerUpdatesListItems { get; set; } = new List<ListItem>();
        public List<Category> ServerUpdatesCategories { get; set; } = new List<Category>();
        public List<Store> ServerUpdatesStores { get; set; } = new List<Store>();
        public List<UserList> ServerUpdatesUserLists { get; set; } = new List<UserList>();

        public List<Guid> ConfirmedDeletions { get; set; } = new List<Guid>(); // IDs of items server confirmed deleted
        public DateTime ServerSyncTimestamp { get; set; } // Server's current time after sync
        public bool HasMoreData { get; set; } // For pagination if needed in the future
        public string? ErrorMessage { get; set; } // To send back any sync errors
    }
}
