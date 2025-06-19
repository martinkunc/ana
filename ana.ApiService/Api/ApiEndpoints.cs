using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ApiEndpoints : IApiEndpoints
{
    private readonly ILogger<ApiEndpoints> _logger;
    //private readonly ApplicationDbContext _applicationDbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public ApiEndpoints(ILogger<ApiEndpoints> logger,
    //IServiceProvider serviceProvider,
    IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _logger = logger;
        //_serviceProvider = serviceProvider;
        _dbContextFactory = dbContextFactory;
    }


    public async Task<CreateGroupResponse> CreateGroup(ClaimsPrincipal user, CreateGroupRequest request)
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
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID not found in claims.");

        _applicationDbContext.AnaGroupToUsers.Add(new AnaGroupToUser
        {
            UserId = userId,
            GroupId = group.Id
        });
        await _applicationDbContext.SaveChangesAsync();

        return new CreateGroupResponse { Group = group };
    }


}

    public class CreateGroupRequest
    {
        public string Name { get; set; }
    }

    public class CreateGroupResponse
    {
        public AnaGroup Group { get; set; }
    }