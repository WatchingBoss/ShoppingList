using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Server.Data;
using ShoppingListApp.Shared.DTOs;
using ShoppingListApp.Shared.Models;

namespace ShoppingListApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly ServerDbContext _context;
        private readonly ILogger<SyncController> _logger;

        public SyncController(ServerDbContext context, ILogger<SyncController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostSync([FromBody] SyncRequestDto requestDto)
        {
            if (requestDto == null)
            {
                return BadRequest("Request DTO cannot be null.");
            }

            _logger.LogInformation("Sync request received. Updating {UpdatedItemsCount} items, Deleting {DeletedItemsCount} items. Client LastSync: {LastSyncTimestamp}",
                requestDto.UpdatedItems.Count, requestDto.DeletedItemIds.Count, requestDto.LastSyncTimestamp);

            var response = new SyncResponseDto
            {
                ServerSyncTimestamp = DateTime.UtcNow
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Process Deletions
                if (requestDto.DeletedItemIds != null && requestDto.DeletedItemIds.Any())
                {
                    foreach (var itemId in requestDto.DeletedItemIds)
                    {
                        var itemToRemove = await _context.ListItems.FindAsync(itemId);
                        if (itemToRemove != null)
                        {
                            _context.ListItems.Remove(itemToRemove);
                            response.ConfirmedDeletions.Add(itemId);
                            _logger.LogInformation("Item {ItemId} deleted.", itemId);
                        }
                        else
                        {
                             _logger.LogWarning("Item {ItemId} for deletion not found.", itemId);
                        }
                    }
                }

                // Process Updates/Creations for ListItems
                if (requestDto.UpdatedItems != null && requestDto.UpdatedItems.Any())
                {
                    foreach (var clientItem in requestDto.UpdatedItems)
                    {
                        var serverItem = await _context.ListItems.FindAsync(clientItem.Id);
                        if (serverItem == null) // Create new item
                        {
                            // Ensure related entities exist or handle appropriately
                            if (!await _context.Categories.AnyAsync(c => c.Id == clientItem.CategoryId))
                                clientItem.CategoryId = _context.Categories.First().Id; // Fallback or error
                            if (!await _context.Stores.AnyAsync(s => s.Id == clientItem.StoreId))
                                clientItem.StoreId = _context.Stores.First().Id; // Fallback or error
                             if (!await _context.UserLists.AnyAsync(u => u.Id == clientItem.UserListId))
                                clientItem.UserListId = _context.UserLists.First().Id; // Fallback or error

                            _context.ListItems.Add(clientItem);
                             _logger.LogInformation("Item {ItemId} ({ItemName}) created.", clientItem.Id, clientItem.Name);
                        }
                        else // Update existing item
                        {
                            // Here you might want to implement more sophisticated conflict resolution.
                            // For now, last write wins (client's changes overwrite server's if IDs match).
                            _context.Entry(serverItem).CurrentValues.SetValues(clientItem);
                             _logger.LogInformation("Item {ItemId} ({ItemName}) updated.", clientItem.Id, clientItem.Name);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully.");

                // Fetch changes from server to send back to client
                // This should ideally be based on requestDto.LastSyncTimestamp
                // For simplicity, sending all items for now. A real app needs proper delta sync.
                response.ServerUpdatesListItems = await _context.ListItems.AsNoTracking().ToListAsync();
                response.ServerUpdatesCategories = await _context.Categories.AsNoTracking().ToListAsync();
                response.ServerUpdatesStores = await _context.Stores.AsNoTracking().ToListAsync();
                response.ServerUpdatesUserLists = await _context.UserLists.AsNoTracking().ToListAsync();

                _logger.LogInformation("Sending {ListItemsCount} ListItems, {CategoriesCount} Categories, {StoresCount} Stores, {UserListsCount} UserLists to client.",
                    response.ServerUpdatesListItems.Count, response.ServerUpdatesCategories.Count, response.ServerUpdatesStores.Count, response.ServerUpdatesUserLists.Count);

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during sync operation. Transaction rolled back.");
                response.ErrorMessage = $"Server error: {ex.Message}";
                return StatusCode(500, response);
            }

            return Ok(response);
        }
    }
}
