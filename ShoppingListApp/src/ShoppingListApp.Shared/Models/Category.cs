using System;

namespace ShoppingListApp.Shared.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
