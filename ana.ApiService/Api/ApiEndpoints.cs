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

        return new CreateGroupResponse { Group = group };
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

        return new GetUserGroupsResponse { UserId=userId, Groups = groups };
    }


}
