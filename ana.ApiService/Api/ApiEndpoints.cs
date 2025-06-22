using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ApiEndpoints : IApiEndpoints
{
    private readonly ILogger<ApiEndpoints> _logger;

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public ApiEndpoints(ILogger<ApiEndpoints> logger,
    //IServiceProvider serviceProvider,
    IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _logger = logger;
        //_serviceProvider = serviceProvider;
        _dbContextFactory = dbContextFactory;
    }


    public async Task<CreateGroupResponse> CreateGroup(CreateGroupRequest request)
    {

        _logger.LogInformation("Creating group with name: {groupName}", request.Name);
        //var _applicationDbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var group = new AnaGroup
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
        };

        _applicationDbContext.AnaGroups.Add(group);

        _applicationDbContext.AnaGroupToUsers.Add(new AnaGroupToUser
        {
            UserId = request.userId,
            GroupId = group.Id
        });
        await _applicationDbContext.SaveChangesAsync();

        return new CreateGroupResponse { UserId = request.userId, Group = group };
    }

    public async Task<GetUserGroupsResponse> GetUserGroups(string userId)
    {

        _logger.LogInformation("Getting groups ");

        //var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found in claims.");

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var userGroupToUsers = await _applicationDbContext.AnaGroupToUsers
            .Where(agu => agu.UserId == userId)
            .Select(gu => gu.GroupId)
            .ToListAsync();

        var groups = await _applicationDbContext.AnaGroups
            .Where(g => userGroupToUsers.Contains(g.Id))
            .ToListAsync();

        return new GetUserGroupsResponse { UserId = userId, Groups = groups };
    }

    public async Task SelectGroup(string userId, string groupId)
    {
        _logger.LogInformation("Selecting group {groupId} for user {userId}", groupId, userId);

        //var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found in claims.");

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var userGroupToUsers = await _applicationDbContext.AnaUsers
            .Where(agu => agu.Id == userId)
            .FirstOrDefaultAsync();
        if (userGroupToUsers == null)
        {
            _applicationDbContext.AnaUsers.Add(new AnaUser
            {
                Id = userId,
                SelectedGroupId = groupId
            });
        }
        else
        {
            userGroupToUsers.SelectedGroupId = groupId;
            _applicationDbContext.AnaUsers.Update(userGroupToUsers);
        }
        await _applicationDbContext.SaveChangesAsync();
        _logger.LogInformation("Group {groupId} selected successfully for user {userId}", groupId, userId);
    }

    public async Task<AnaGroup> GetSelectedGroup(string userId)
    {
        _logger.LogInformation("Getting selected group for user {userId}", userId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var user = await _applicationDbContext.AnaUsers
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (user == null || string.IsNullOrEmpty(user.SelectedGroupId))
        {
            var userGroups = await _applicationDbContext.AnaGroupToUsers
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (userGroups == null)
            {
                return null;
            }

            var firstGroupId = userGroups.GroupId;

            var firstGroup = await _applicationDbContext.AnaGroups
                .Where(u => u.Id == firstGroupId)
                .FirstOrDefaultAsync();
            return firstGroup;
        }

        var group = await _applicationDbContext.AnaGroups
            .Where(g => g.Id == user.SelectedGroupId)
            .FirstOrDefaultAsync();
        return group;
    }

    public async Task<List<AnaAnniv>> GetAnniversaries(string groupId)
    {
        _logger.LogInformation("Getting anniversaries for group {groupId}", groupId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var anniversaries = await _applicationDbContext.AnaAnnivs
            .Where(agu => agu.GroupId == groupId)
            .OrderBy(agu => agu.AlignedDate)
            .ToListAsync();

        return anniversaries;
    }

    public async Task<CreateAnniversaryResponse> CreateAnniversary(string groupId, AnaAnniv anniversary)
    {
        _logger.LogInformation("Create anniversary for group {groupId}", groupId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();
        if (string.IsNullOrEmpty(anniversary.Id))
        {
            anniversary.Id = Guid.NewGuid().ToString();
        }
        anniversary.AlignedDate = GetAlignedDate(anniversary.Date);

        var na = _applicationDbContext.AnaAnnivs.Add(anniversary);
        await _applicationDbContext.SaveChangesAsync();
        return new CreateAnniversaryResponse
        {
            GroupId = groupId,
            Anniversary = na.Entity
        };
    }

    public async Task<AnaAnniv> UpdateAnniversary(string groupId, string anniversaryId, AnaAnniv anniversary)
    {
        _logger.LogInformation("Create anniversary for group {groupId}", groupId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();
        if (string.IsNullOrEmpty(anniversary.Id))
        {
            throw new ArgumentException("Anniversary ID cannot be null or empty.");
        }
        anniversary.AlignedDate = GetAlignedDate(anniversary.Date);

        AnaAnniv existingAnniversary = null;

        existingAnniversary = await _applicationDbContext
            .AnaAnnivs
            .FirstOrDefaultAsync(a => a.Id == anniversaryId && a.GroupId == groupId);
            
        if (existingAnniversary == null)
        {
            existingAnniversary = (await _applicationDbContext
                .AnaAnnivs.AddAsync(anniversary)).Entity;
        }
        existingAnniversary.Name = anniversary.Name;
        existingAnniversary.Date = anniversary.Date;
        existingAnniversary.AlignedDate = anniversary.AlignedDate;

        _applicationDbContext
                .AnaAnnivs.Update(existingAnniversary);

        await _applicationDbContext.SaveChangesAsync();
        return existingAnniversary;
    }

    public async Task DeleteAnniversary(string groupId, string anniversaryId)
    {
        _logger.LogInformation("Create anniversary for group {groupId}", groupId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();
        if (string.IsNullOrEmpty(anniversaryId) || string.IsNullOrEmpty(anniversaryId))
        {
            throw new ArgumentException("Anniversary ID or Group Id cannot be null or empty.");
        }
        var existingAnniversary = await _applicationDbContext
            .AnaAnnivs
            .FirstOrDefaultAsync(a => a.Id == anniversaryId && a.GroupId == groupId);

        if (existingAnniversary != null)
        {
            _applicationDbContext.Remove(existingAnniversary);
            await _applicationDbContext.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning("Anniversary with ID {anniversaryId} not found in group {groupId}", anniversaryId, groupId);
            throw new KeyNotFoundException($"Anniversary with ID {anniversaryId} not found in group {groupId}");
        }
    }
    
    public string GetAlignedDate(string date)
    {
        var parts = date.Split('/');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int day) &&
            int.TryParse(parts[1], out int month))
        {
            string alignedDay = day.ToString("D2"); // "01"
            string alignedMonth = month.ToString("D2"); // "01"
            return $"{alignedMonth}{alignedDay}";
        }
        else
        {
            throw new ArgumentException("Date must be in day/month format (e.g., 15/3 or 01/12)");
        }
    }

}
