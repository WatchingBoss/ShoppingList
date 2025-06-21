using System;

namespace ShoppingListApp.Shared.Models
{
    public class ListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public Guid StoreId { get; set; }
        public PurchaseType PurchaseType { get; set; }
        public bool IsRecurring { get; set; }
        public bool IsActive { get; set; }
        public bool IsArchived { get; set; }
        public Guid UserListId { get; set; }
    }

    public enum PurchaseType
    {
        Online,
        Offline
    }
}
