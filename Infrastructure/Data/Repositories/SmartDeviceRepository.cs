using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Infrastructure.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NexusHome.IoT.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for SmartDevice entities with advanced querying and caching capabilities
/// Provides optimized database access patterns and comprehensive CRUD operations
/// Implements unit of work pattern for transactional consistency
/// </summary>
public class SmartDeviceRepository : BaseRepository<SmartDevice>, ISmartDeviceRepository
{
    private readonly SmartHomeDbContext _databaseContext;
    private readonly ILogger<SmartDeviceRepository> _logger;

    public SmartDeviceRepository(
        SmartHomeDbContext databaseContext,
        ILogger<SmartDeviceRepository> logger) : base(databaseContext, logger)
    {
        _databaseContext = databaseContext;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves device by unique device identifier with optional related data loading
    /// </summary>
    /// <param name="deviceIdentifier">Unique device identifier string</param>
    /// <param name="includeRelatedData">Whether to load related entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Smart device entity or null if not found</returns>
    public async Task<SmartDevice?> GetByDeviceIdentifierAsync(string deviceIdentifier, 
        bool includeRelatedData = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _databaseContext.SmartDevices.AsNoTracking();

            if (includeRelatedData)
            {
                query = query
                    .Include(device => device.EnergyReadings.OrderByDescending(reading => reading.Timestamp).Take(50))
                    .Include(device => device.MaintenanceRecords.OrderByDescending(record => record.CreatedAt).Take(10))
                    .Include(device => device.DeviceAlerts.Where(alert => !alert.HasBeenRead).Take(20));
            }

            var result = await query
                .FirstOrDefaultAsync(device => device.DeviceId == deviceIdentifier, cancellationToken);

            if (result != null)
            {
                _logger.LogDebug("Retrieved device {DeviceIdentifier} with related data: {IncludeRelated}",
                    deviceIdentifier, includeRelatedData);
            }

            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve device by identifier {DeviceIdentifier}", deviceIdentifier);
            throw;
        }
    }

    /// <summary>
    /// Gets devices filtered by operational status with pagination support
    /// </summary>
    /// <param name="operationalStatus">Target operational status</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated collection of devices matching status criteria</returns>
    public async Task<PaginatedResult<SmartDevice>> GetDevicesByOperationalStatusAsync(
        DeviceOperationalStatus operationalStatus, int pageNumber = 1, int pageSize = 50, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _databaseContext.SmartDevices
                .Where(device => device.CurrentStatus == operationalStatus)
                .OrderBy(device => device.DeviceFriendlyName)
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);
            
            var devices = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {DeviceCount} devices with status {Status} (page {Page} of {TotalPages})",
                devices.Count, operationalStatus, pageNumber, (int)Math.Ceiling((double)totalCount / pageSize));

            return new PaginatedResult<SmartDevice>
            {
                Items = devices,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve devices by operational status {Status}", operationalStatus);
            throw;
        }
    }

    /// <summary>
    /// Retrieves devices by category with advanced filtering options
    /// </summary>
    /// <param name="deviceCategory">Device category filter</param>
    /// <param name="onlineOnly">Include only online devices</param>
    /// <param name="locationFilter">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of devices matching criteria</returns>
    public async Task<IEnumerable<SmartDevice>> GetDevicesByCategoryAsync(DeviceCategory deviceCategory,
        bool onlineOnly = false, string? locationFilter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _databaseContext.SmartDevices
                .Where(device => device.DeviceType == deviceCategory)
                .AsNoTracking();

            if (onlineOnly)
            {
                query = query.Where(device => device.IsCurrentlyOnline);
            }

            if (!string.IsNullOrEmpty(locationFilter))
            {
                query = query.Where(device => device.PhysicalLocation != null && 
                    device.PhysicalLocation.Contains(locationFilter));
            }

            var devices = await query
                .OrderBy(device => device.PhysicalLocation)
                .ThenBy(device => device.DeviceFriendlyName)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {DeviceCount} devices for category {Category} with filters - Online: {OnlineOnly}, Location: {Location}",
                devices.Count, deviceCategory, onlineOnly, locationFilter ?? "None");

            return devices;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve devices by category {Category}", deviceCategory);
            throw;
        }
    }

    /// <summary>
    /// Gets devices that haven't communicated within specified timespan
    /// </summary>
    /// <param name="offlineThreshold">Time threshold for considering device offline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of potentially offline devices</returns>
    public async Task<IEnumerable<SmartDevice>> GetPotentiallyOfflineDevicesAsync(TimeSpan offlineThreshold,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(offlineThreshold);

            var offlineDevices = await _databaseContext.SmartDevices
                .Where(device => device.LastCommunicationTime < cutoffTime || !device.IsCurrentlyOnline)
                .OrderBy(device => device.LastCommunicationTime)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {OfflineDeviceCount} potentially offline devices (threshold: {ThresholdMinutes} minutes)",
                offlineDevices.Count, offlineThreshold.TotalMinutes);

            return offlineDevices;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve potentially offline devices");
            throw;
        }
    }

    /// <summary>
    /// Retrieves devices with high power consumption above specified threshold
    /// </summary>
    /// <param name="powerThresholdWatts">Power consumption threshold in watts</param>
    /// <param name="includePowerHistory">Whether to include recent power consumption history</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of high power consumption devices</returns>
    public async Task<IEnumerable<SmartDevice>> GetHighPowerConsumptionDevicesAsync(decimal powerThresholdWatts,
        bool includePowerHistory = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _databaseContext.SmartDevices
                .Where(device => device.CurrentPowerConsumption > powerThresholdWatts)
                .AsNoTracking();

            if (includePowerHistory)
            {
                query = query.Include(device => device.EnergyReadings
                    .OrderByDescending(reading => reading.Timestamp)
                    .Take(100));
            }

            var highConsumptionDevices = await query
                .OrderByDescending(device => device.CurrentPowerConsumption)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {DeviceCount} devices with power consumption above {ThresholdWatts}W",
                highConsumptionDevices.Count, powerThresholdWatts);

            return highConsumptionDevices;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve high power consumption devices");
            throw;
        }
    }

    /// <summary>
    /// Updates device online status and last communication timestamp
    /// </summary>
    /// <param name="deviceIdentifier">Device identifier</param>
    /// <param name="isOnline">New online status</param>
    /// <param name="lastCommunicationTime">Timestamp of last communication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if device was updated, false if device not found</returns>
    public async Task<bool> UpdateDeviceOnlineStatusAsync(string deviceIdentifier, bool isOnline,
        DateTime? lastCommunicationTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var device = await _databaseContext.SmartDevices
                .FirstOrDefaultAsync(d => d.UniqueDeviceIdentifier == deviceIdentifier, cancellationToken);

            if (device == null)
            {
                _logger.LogWarning("Device {DeviceIdentifier} not found for status update", deviceIdentifier);
                return false;
            }

            var previousStatus = device.IsCurrentlyOnline;
            device.IsCurrentlyOnline = isOnline;
            device.LastCommunicationTime = lastCommunicationTime ?? DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;

            await _databaseContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated device {DeviceIdentifier} online status from {PreviousStatus} to {NewStatus}",
                deviceIdentifier, previousStatus, isOnline);

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update device online status for {DeviceIdentifier}", deviceIdentifier);
            throw;
        }
    }

    /// <summary>
    /// Bulk updates power consumption for multiple devices efficiently
    /// </summary>
    /// <param name="powerConsumptionUpdates">Dictionary of device ID to power consumption values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of devices successfully updated</returns>
    public async Task<int> BulkUpdatePowerConsumptionAsync(Dictionary<string, decimal> powerConsumptionUpdates,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deviceIdentifiers = powerConsumptionUpdates.Keys.ToList();
            var devicesToUpdate = await _databaseContext.SmartDevices
                .Where(device => deviceIdentifiers.Contains(device.UniqueDeviceIdentifier))
                .ToListAsync(cancellationToken);

            var updatedCount = 0;
            var updateTimestamp = DateTime.UtcNow;

            foreach (var device in devicesToUpdate)
            {
                if (powerConsumptionUpdates.TryGetValue(device.UniqueDeviceIdentifier, out var newPowerConsumption))
                {
                    device.CurrentPowerConsumption = newPowerConsumption;
                    device.LastCommunicationTime = updateTimestamp;
                    device.UpdatedAt = updateTimestamp;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _databaseContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Bulk updated power consumption for {UpdatedCount} devices", updatedCount);
            }

            return updatedCount;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to perform bulk power consumption update");
            throw;
        }
    }

    /// <summary>
    /// Searches devices using flexible criteria with full-text capabilities
    /// </summary>
    /// <param name="searchCriteria">Search parameters and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results with matching devices</returns>
    public async Task<DeviceSearchResult> SearchDevicesAsync(DeviceSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _databaseContext.SmartDevices.AsQueryable();

            // Apply text search filter
            if (!string.IsNullOrEmpty(searchCriteria.SearchText))
            {
                var searchTerms = searchCriteria.SearchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    query = query.Where(device => 
                        device.DeviceFriendlyName.Contains(term) ||
                        device.ManufacturerName.Contains(term) ||
                        device.ModelNumber.Contains(term) ||
                        (device.PhysicalLocation != null && device.PhysicalLocation.Contains(term)) ||
                        (device.DeviceDescription != null && device.DeviceDescription.Contains(term)));
                }
            }

            // Apply category filter
            if (searchCriteria.DeviceCategories?.Any() == true)
            {
                query = query.Where(device => searchCriteria.DeviceCategories.Contains(device.DeviceType));
            }

            // Apply online status filter
            if (searchCriteria.OnlineStatusFilter.HasValue)
            {
                query = query.Where(device => device.IsCurrentlyOnline == searchCriteria.OnlineStatusFilter.Value);
            }

            // Apply power consumption range filter
            if (searchCriteria.MinPowerConsumption.HasValue)
            {
                query = query.Where(device => device.CurrentPowerConsumption >= searchCriteria.MinPowerConsumption.Value);
            }

            if (searchCriteria.MaxPowerConsumption.HasValue)
            {
                query = query.Where(device => device.CurrentPowerConsumption <= searchCriteria.MaxPowerConsumption.Value);
            }

            // Apply location filters
            if (searchCriteria.LocationFilters?.Any() == true)
            {
                query = query.Where(device => device.PhysicalLocation != null &&
                    searchCriteria.LocationFilters.Any(location => device.PhysicalLocation.Contains(location)));
            }

            // Apply date range filter for last communication
            if (searchCriteria.LastCommunicationAfter.HasValue)
            {
                query = query.Where(device => device.LastCommunicationTime >= searchCriteria.LastCommunicationAfter.Value);
            }

            if (searchCriteria.LastCommunicationBefore.HasValue)
            {
                query = query.Where(device => device.LastCommunicationTime <= searchCriteria.LastCommunicationBefore.Value);
            }

            // Apply sorting
            query = searchCriteria.SortBy?.ToLowerInvariant() switch
            {
                "name" => searchCriteria.SortDescending 
                    ? query.OrderByDescending(device => device.DeviceFriendlyName)
                    : query.OrderBy(device => device.DeviceFriendlyName),
                "location" => searchCriteria.SortDescending
                    ? query.OrderByDescending(device => device.PhysicalLocation)
                    : query.OrderBy(device => device.PhysicalLocation),
                "power" => searchCriteria.SortDescending
                    ? query.OrderByDescending(device => device.CurrentPowerConsumption)
                    : query.OrderBy(device => device.CurrentPowerConsumption),
                "lastcommunication" => searchCriteria.SortDescending
                    ? query.OrderByDescending(device => device.LastCommunicationTime)
                    : query.OrderBy(device => device.LastCommunicationTime),
                _ => query.OrderBy(device => device.DeviceFriendlyName)
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var devices = await query
                .Skip((searchCriteria.PageNumber - 1) * searchCriteria.PageSize)
                .Take(searchCriteria.PageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var searchResult = new DeviceSearchResult
            {
                Devices = devices,
                TotalCount = totalCount,
                PageNumber = searchCriteria.PageNumber,
                PageSize = searchCriteria.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / searchCriteria.PageSize),
                SearchCriteria = searchCriteria,
                SearchExecutedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Device search completed: found {TotalCount} devices, returning page {PageNumber} with {DeviceCount} devices",
                totalCount, searchCriteria.PageNumber, devices.Count);

            return searchResult;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to execute device search");
            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive device statistics for dashboard and reporting
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Statistical summary of all devices</returns>
    public async Task<DeviceStatisticsSummary> GetDeviceStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allDevices = await _databaseContext.SmartDevices
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var statistics = new DeviceStatisticsSummary
            {
                TotalDeviceCount = allDevices.Count,
                OnlineDeviceCount = allDevices.Count(d => d.IsCurrentlyOnline),
                OfflineDeviceCount = allDevices.Count(d => !d.IsCurrentlyOnline),
                TotalPowerConsumptionWatts = allDevices.Sum(d => d.CurrentPowerConsumption),
                AveragePowerConsumptionWatts = allDevices.Any() ? allDevices.Average(d => d.CurrentPowerConsumption) : 0,
                DevicesByCategory = allDevices.GroupBy(d => d.DeviceType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                DevicesByLocation = allDevices
                    .Where(d => !string.IsNullOrEmpty(d.PhysicalLocation))
                    .GroupBy(d => d.PhysicalLocation!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DevicesByOperationalStatus = allDevices.GroupBy(d => d.CurrentStatus)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                LastCalculatedAt = DateTime.UtcNow
            };

            // Calculate additional metrics
            if (allDevices.Any())
            {
                statistics.HighestPowerConsumptionWatts = allDevices.Max(d => d.CurrentPowerConsumption);
                statistics.DeviceWithHighestConsumption = allDevices
                    .OrderByDescending(d => d.CurrentPowerConsumption)
                    .First().DeviceFriendlyName;

                var recentlySeenThreshold = DateTime.UtcNow.AddMinutes(-30);
                statistics.RecentlyActiveDeviceCount = allDevices
                    .Count(d => d.LastCommunicationTime >= recentlySeenThreshold);
            }

            _logger.LogInformation("Generated device statistics: {TotalCount} total devices, {OnlineCount} online, {OfflineCount} offline",
                statistics.TotalDeviceCount, statistics.OnlineDeviceCount, statistics.OfflineDeviceCount);

            return statistics;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to generate device statistics");
            throw;
        }
    }
}

/// <summary>
/// Base repository implementation providing common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class BaseRepository<T> where T : class
{
    protected readonly SmartHomeDbContext DatabaseContext;
    protected readonly ILogger Logger;
    protected readonly DbSet<T> DbSet;

    protected BaseRepository(SmartHomeDbContext databaseContext, ILogger logger)
    {
        DatabaseContext = databaseContext;
        Logger = logger;
        DbSet = databaseContext.Set<T>();
    }

    /// <summary>
    /// Gets entity by primary key with optional related data loading
    /// </summary>
    /// <param name="id">Primary key value</param>
    /// <param name="includeProperties">Related properties to include</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity or null if not found</returns>
    public virtual async Task<T?> GetByIdAsync(object id, string[]? includeProperties = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = DbSet.AsQueryable();

            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }

            // Assume the entity has an Id property
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, "Id");
            var constant = Expression.Constant(id);
            var equality = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

            return await query.FirstOrDefaultAsync(lambda, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to retrieve entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Gets all entities with optional filtering and related data loading
    /// </summary>
    /// <param name="filter">Optional filter expression</param>
    /// <param name="includeProperties">Related properties to include</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of entities</returns>
    public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null,
        string[]? includeProperties = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = DbSet.AsQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }

            return await query.AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to retrieve entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Adds new entity to the repository
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added entity</returns>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var addedEntity = await DbSet.AddAsync(entity, cancellationToken);
            return addedEntity.Entity;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to add entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Updates existing entity in the repository
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <returns>Updated entity</returns>
    public virtual T Update(T entity)
    {
        try
        {
            var updatedEntity = DbSet.Update(entity);
            return updatedEntity.Entity;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to update entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Removes entity from the repository
    /// </summary>
    /// <param name="entity">Entity to remove</param>
    public virtual void Remove(T entity)
    {
        try
        {
            DbSet.Remove(entity);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to remove entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Removes entity by primary key
    /// </summary>
    /// <param name="id">Primary key value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if entity was removed, false if not found</returns>
    public virtual async Task<bool> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
            if (entity == null)
            {
                return false;
            }

            Remove(entity);
            return true;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to remove entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Gets count of entities matching optional filter
    /// </summary>
    /// <param name="filter">Optional filter expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of matching entities</returns>
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = DbSet.AsQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to count entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Checks if any entities match the filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entities match, false otherwise</returns>
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await DbSet.AnyAsync(filter, cancellationToken);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to check existence of entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }
}

/// <summary>
/// Paginated result container for repository queries
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Device search criteria for advanced filtering
/// </summary>
public class DeviceSearchCriteria
{
    public string? SearchText { get; set; }
    public List<DeviceCategory>? DeviceCategories { get; set; }
    public bool? OnlineStatusFilter { get; set; }
    public decimal? MinPowerConsumption { get; set; }
    public decimal? MaxPowerConsumption { get; set; }
    public List<string>? LocationFilters { get; set; }
    public DateTime? LastCommunicationAfter { get; set; }
    public DateTime? LastCommunicationBefore { get; set; }
    public string? SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Device search result container
/// </summary>
public class DeviceSearchResult
{
    public List<SmartDevice> Devices { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public DeviceSearchCriteria SearchCriteria { get; set; } = new();
    public DateTime SearchExecutedAt { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Device statistics summary for dashboard display
/// </summary>
public class DeviceStatisticsSummary
{
    public int TotalDeviceCount { get; set; }
    public int OnlineDeviceCount { get; set; }
    public int OfflineDeviceCount { get; set; }
    public int RecentlyActiveDeviceCount { get; set; }
    public decimal TotalPowerConsumptionWatts { get; set; }
    public decimal AveragePowerConsumptionWatts { get; set; }
    public decimal HighestPowerConsumptionWatts { get; set; }
    public string? DeviceWithHighestConsumption { get; set; }
    public Dictionary<string, int> DevicesByCategory { get; set; } = new();
    public Dictionary<string, int> DevicesByLocation { get; set; } = new();
    public Dictionary<string, int> DevicesByOperationalStatus { get; set; } = new();
    public DateTime LastCalculatedAt { get; set; }
}

/// <summary>
/// Interface for SmartDevice repository operations
/// </summary>
public interface ISmartDeviceRepository
{
    Task<SmartDevice?> GetByDeviceIdentifierAsync(string deviceIdentifier, bool includeRelatedData = false, CancellationToken cancellationToken = default);
    Task<PaginatedResult<SmartDevice>> GetDevicesByOperationalStatusAsync(DeviceOperationalStatus operationalStatus, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<SmartDevice>> GetDevicesByCategoryAsync(DeviceCategory deviceCategory, bool onlineOnly = false, string? locationFilter = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SmartDevice>> GetPotentiallyOfflineDevicesAsync(TimeSpan offlineThreshold, CancellationToken cancellationToken = default);
    Task<IEnumerable<SmartDevice>> GetHighPowerConsumptionDevicesAsync(decimal powerThresholdWatts, bool includePowerHistory = false, CancellationToken cancellationToken = default);
    Task<bool> UpdateDeviceOnlineStatusAsync(string deviceIdentifier, bool isOnline, DateTime? lastCommunicationTime = null, CancellationToken cancellationToken = default);
    Task<int> BulkUpdatePowerConsumptionAsync(Dictionary<string, decimal> powerConsumptionUpdates, CancellationToken cancellationToken = default);
    Task<DeviceSearchResult> SearchDevicesAsync(DeviceSearchCriteria searchCriteria, CancellationToken cancellationToken = default);
    Task<DeviceStatisticsSummary> GetDeviceStatisticsAsync(CancellationToken cancellationToken = default);
}
