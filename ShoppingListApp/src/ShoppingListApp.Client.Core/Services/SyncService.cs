using ShoppingListApp.Client.Core.Data;
using ShoppingListApp.Shared.DTOs;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Added for ILogger

namespace ShoppingListApp.Client.Core.Services
{
    public class SyncService
    {
        private readonly HttpClient _httpClient; // Injected HttpClient for API calls
        private readonly ShoppingRepository _repository;
        private readonly ILogger<SyncService> _logger;
        private const string ServerSyncEndpoint = "api/Sync"; // Define the server endpoint

        public SyncService(HttpClient httpClient, ShoppingRepository repository, ILogger<SyncService> logger)
        {
            _httpClient = httpClient;
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> PerformSyncAsync()
        {
            _logger.LogInformation("Starting synchronization process...");

            try
            {
                // 1. Get local changes to send to server
                DateTime lastSyncTimestamp = await _repository.GetLastSyncTimestampAsync();
                _logger.LogInformation("Last sync timestamp from local DB: {LastSyncTimestamp}", lastSyncTimestamp);

                // In a real app, you'd collect actual local changes (new, updated, deleted items)
                // For this example, ShoppingRepository.GetLocalChangesForSyncAsync is simplified
                var localChanges = await _repository.GetLocalChangesForSyncAsync(lastSyncTimestamp);
                _logger.LogInformation("Prepared local changes: {UpdatedItemsCount} items to update/create, {DeletedItemIdsCount} items to delete.",
                                   localChanges.UpdatedItems.Count, localChanges.DeletedItemIds.Count);


                // 2. Send local changes to server and get server changes
                _logger.LogInformation("Sending local changes to server at {Endpoint}...", ServerSyncEndpoint);
                HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(ServerSyncEndpoint, localChanges);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Sync request failed. Status: {StatusCode}. Response: {ErrorContent}", httpResponse.StatusCode, errorContent);
                    // Optionally, inspect response.ErrorMessage if it's populated by the server on error
                    return false;
                }

                var serverResponse = await httpResponse.Content.ReadFromJsonAsync<SyncResponseDto>();
                if (serverResponse == null)
                {
                    _logger.LogError("Failed to deserialize server response.");
                    return false;
                }
                _logger.LogInformation("Received server response. Server Timestamp: {ServerSyncTimestamp}. Updates: {ListItemsCount} ListItems, {CategoriesCount} Categories, {StoresCount} Stores, {UserListsCount} UserLists. Confirmed Deletions: {ConfirmedDeletionsCount}.",
                    serverResponse.ServerSyncTimestamp, serverResponse.ServerUpdatesListItems.Count, serverResponse.ServerUpdatesCategories.Count, serverResponse.ServerUpdatesStores.Count, serverResponse.ServerUpdatesUserLists.Count, serverResponse.ConfirmedDeletions.Count);


                if (!string.IsNullOrEmpty(serverResponse.ErrorMessage))
                {
                    _logger.LogError("Server returned an error during sync: {ErrorMessage}", serverResponse.ErrorMessage);
                    return false;
                }

                // 3. Apply server changes to local database
                _logger.LogInformation("Applying server changes to local database...");
                await _repository.UpsertCategoriesAsync(serverResponse.ServerUpdatesCategories);
                _logger.LogInformation("{Count} categories upserted.", serverResponse.ServerUpdatesCategories.Count);

                await _repository.UpsertStoresAsync(serverResponse.ServerUpdatesStores);
                _logger.LogInformation("{Count} stores upserted.", serverResponse.ServerUpdatesStores.Count);

                await _repository.UpsertUserListsAsync(serverResponse.ServerUpdatesUserLists);
                _logger.LogInformation("{Count} user lists upserted.", serverResponse.ServerUpdatesUserLists.Count);

                await _repository.UpsertListItemsAsync(serverResponse.ServerUpdatesListItems);
                _logger.LogInformation("{Count} list items upserted.", serverResponse.ServerUpdatesListItems.Count);

                // Confirm deletions locally if any were processed by the server
                if (serverResponse.ConfirmedDeletions.Any())
                {
                    // This step might be redundant if the client already marked them as deleted
                    // and expects them to be gone after sync. However, it's good for ensuring consistency.
                    // For now, the repository's GetLocalChangesForSyncAsync would need to handle not re-sending these.
                    _logger.LogInformation("Server confirmed deletion of {Count} items. (Local deletion logic might need refinement based on strategy)", serverResponse.ConfirmedDeletions.Count);
                }

                // 4. Update last sync timestamp
                await _repository.SetLastSyncTimestampAsync(serverResponse.ServerSyncTimestamp);
                _logger.LogInformation("Synchronization successful. Last sync timestamp updated to: {ServerSyncTimestamp}", serverResponse.ServerSyncTimestamp);

                return true;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error during sync.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during synchronization.");
                return false;
            }
        }
    }
}
