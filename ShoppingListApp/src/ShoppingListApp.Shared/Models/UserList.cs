using System;

namespace ShoppingListApp.Shared.Models
{
    public class UserList
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
