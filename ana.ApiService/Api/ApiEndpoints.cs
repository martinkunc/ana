using System.Security.Claims;
using ana.SharedNet;

public class ApiEndpoints : IApiEndpoints
{
    private readonly ILogger<ApiEndpoints> _logger;

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DailyTaskService _dailyTaskService;

    public ApiEndpoints(ILogger<ApiEndpoints> logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IHttpContextAccessor httpContextAccessor,
        DailyTaskService dailyTaskService)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _httpContextAccessor = httpContextAccessor;
        _dailyTaskService = dailyTaskService;
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

        var adminRole = await _applicationDbContext.AnaRoles
                .Where(r => r.Name == AnaRoleNames.Admin)
                .FirstOrDefaultAsync();
        if (adminRole == null)
            return null;

        _applicationDbContext.AnaGroupToUsers.Add(new AnaGroupToUser
        {
            UserId = request.userId,
            GroupId = group.Id,
            RoleId = adminRole.Id
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

    public async Task CreateUser(AnaUser user)
    {
        _logger.LogInformation("Creating user {userId}", user.Id);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();
        var existingUser = await _applicationDbContext.AnaUsers
            .Where(agu => agu.Id == user.Id)
            .FirstOrDefaultAsync();
        if (existingUser != null) throw new InvalidOperationException("The user already exists.");
        _applicationDbContext.AnaUsers.Add(user);
        await _applicationDbContext.SaveChangesAsync();
        _logger.LogInformation("User {userId} created  successfully ", user.Id);

    }
    public async Task SelectGroup(string userId, string groupId)
    {
        _logger.LogInformation("Selecting group {groupId} for user {userId}", groupId, userId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var userGroupToUsers = await _applicationDbContext.AnaUsers
            .Where(agu => agu.Id == userId)
            .FirstOrDefaultAsync();
        if (userGroupToUsers == null)
        {
            throw new InvalidOperationException("The user doesn't exist.");
        }

        userGroupToUsers.SelectedGroupId = groupId;
        _applicationDbContext.AnaUsers.Update(userGroupToUsers);
        await _applicationDbContext.SaveChangesAsync();
        _logger.LogInformation("Group {groupId} selected successfully for user {userId}", groupId, userId);
    }

    public async Task<GetSelectedGroupResponse?> GetSelectedGroup(string userId)
    {
        _logger.LogInformation("Getting selected group for user {userId}", userId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var user = await _applicationDbContext.AnaUsers
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (user == null || string.IsNullOrEmpty(user.SelectedGroupId))
        {
            var userGroup = await _applicationDbContext.AnaGroupToUsers
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (userGroup == null)
            {
                return null;
            }

            var firstGroupId = userGroup.GroupId;
            var firstGroupRid = userGroup.RoleId;

            var firstGroup = await _applicationDbContext.AnaGroups
                .Where(u => u.Id == firstGroupId)
                .FirstOrDefaultAsync();
            if (firstGroup == null)
                return null;

            var role = await _applicationDbContext.AnaRoles
                .Where(r => r.Id == firstGroupRid)
                .FirstOrDefaultAsync();
            if (role == null)
                return null;

            return new GetSelectedGroupResponse { AnaGroup = firstGroup, UserRole = role.Name };
        }

        var group = await _applicationDbContext.AnaGroups
            .Where(g => g.Id == user.SelectedGroupId)
            .FirstOrDefaultAsync();
        if (group == null)
        {
            _logger.LogWarning("Group with ID {groupId} not found for user {userId}", user.SelectedGroupId, userId);
            return null;
        }
        var selectedUserGroup = await _applicationDbContext.AnaGroupToUsers
                .Where(u => u.UserId == userId && u.GroupId == user.SelectedGroupId)
                .FirstOrDefaultAsync();
        var selectedGroupRid = selectedUserGroup?.RoleId;
        if (selectedGroupRid == null)
        {
            return null;
        }

        var selectedRole = await _applicationDbContext.AnaRoles
            .Where(r => r.Id == selectedGroupRid)
            .FirstOrDefaultAsync();
        if (selectedRole == null)
            return null;

        return new GetSelectedGroupResponse { AnaGroup = group, UserRole = selectedRole.Name };
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

    public async Task<List<AnaGroupMember>> GetGroupMembers(string groupId)
    {
        _logger.LogInformation("Getting members for group {groupId}", groupId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var groupToUsers = await _applicationDbContext.AnaGroupToUsers
            .Where(agu => agu.GroupId == groupId)
            .ToListAsync();

        if (groupToUsers == null || !groupToUsers.Any())
            return [];

        var groupMembers = await _applicationDbContext.AnaUsers
            .Where(agu => groupToUsers.Select(g => g.UserId).Contains(agu.Id))
            .ToListAsync();

        var roleIdMap = await _applicationDbContext.AnaRoles
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        var mappedgroupMembers = groupMembers.Select(u => new AnaGroupMember
        {
            UserId = u.Id,
            GroupId = groupId,
            Role = roleIdMap[groupToUsers.FirstOrDefault(gtu => gtu.UserId == u.Id && gtu.GroupId == groupId)?.RoleId ?? string.Empty],
            DisplayName = u.DisplayName,
        }).ToList();

        if (groupMembers == null || !groupMembers.Any())
            return [];

        return mappedgroupMembers;
    }

    public async Task CreateGroupMember(string groupId, AnaGroupMember newMember)
    {
        _logger.LogInformation("Create member for group {groupId}", groupId);
        var creatingUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var _applicationDbContext = _dbContextFactory.CreateDbContext();
        var existingUser = await _applicationDbContext.Users
            .FirstOrDefaultAsync(u => u.Email == newMember.Email);

        if (existingUser == null)
            throw new InvalidOperationException($"User with email {newMember.Email} does not exist.");

        var creatingUserUserGroup = await _applicationDbContext.AnaGroupToUsers
                .Where(u => u.UserId == creatingUserId && u.GroupId == groupId)
                .FirstOrDefaultAsync();

        if (creatingUserUserGroup == null)
            throw new InvalidOperationException("The creating user doesn't exist in the group to where member is being added.");
        var adminRole = await _applicationDbContext.AnaRoles
                .Where(r => r.Name == AnaRoleNames.Admin)
                .FirstOrDefaultAsync();
        if (adminRole == null)
            throw new InvalidOperationException("The Admin role was not found in the system");
        if (creatingUserUserGroup.RoleId != adminRole.Id)
            throw new InvalidOperationException("The user adding the new member is not an admin of the group");


        var newUserRoleId = await _applicationDbContext.AnaRoles
            .Where(r => r.Name == newMember.Role)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var newGroupToUser = new AnaGroupToUser
        {
            UserId = existingUser.Id,
            GroupId = groupId,
            RoleId = newUserRoleId
        };

        var existingGroupToUser = await _applicationDbContext.AnaGroupToUsers
            .FirstOrDefaultAsync(agu => agu.UserId == existingUser.Id && agu.GroupId == groupId);

        if (existingGroupToUser != null)
        {
            _logger.LogWarning("User {userId} is already a member of group {groupId}", existingUser.Id, groupId);
            throw new InvalidOperationException($"User {existingUser.Email} is already a member of group {groupId}");
        }

        _applicationDbContext.AnaGroupToUsers.Add(newGroupToUser);
        await _applicationDbContext.SaveChangesAsync();
    }

    public async Task ChangeGroupMemberRole(string groupId, string userId, ChangeGroupMemberRoleRequest req)
    {
        _logger.LogInformation("Create member for group {groupId}", groupId);
        var creatingUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var creatingUserUserGroup = await _applicationDbContext.AnaGroupToUsers
                .Where(u => u.UserId == creatingUserId && u.GroupId == groupId)
                .FirstOrDefaultAsync();

        if (creatingUserUserGroup == null)
            throw new InvalidOperationException("The creating user doesn't exist in the group where the role of member is being changed.");
        var adminRole = await _applicationDbContext.AnaRoles
                .Where(r => r.Name == AnaRoleNames.Admin)
                .FirstOrDefaultAsync();
        if (adminRole == null)
            throw new InvalidOperationException("The Admin role was not found in the system");
        if (creatingUserUserGroup.RoleId != adminRole.Id)
            throw new InvalidOperationException("The user changing the member role is not an admin of the group");

        var newRoleId = await _applicationDbContext.AnaRoles
            .Where(r => r.Name == req.RoleName)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var groupToUser = await _applicationDbContext.AnaGroupToUsers
            .FirstOrDefaultAsync(agu => agu.UserId == userId && agu.GroupId == groupId);

        if (groupToUser == null)
        {
            groupToUser = new AnaGroupToUser
            {
                UserId = userId,
                GroupId = groupId,
                RoleId = newRoleId
            };
            _applicationDbContext.AnaGroupToUsers.Add(groupToUser);
        }
        else
        {
            groupToUser.RoleId = newRoleId;
            _applicationDbContext.AnaGroupToUsers.Update(groupToUser);
        }
        await _applicationDbContext.SaveChangesAsync();
    }

    public async Task DeleteGroupMember(string groupId, string userId)
    {
        _logger.LogInformation("Create member for group {groupId}", groupId);
        var creatingUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var creatingUserUserGroup = await _applicationDbContext.AnaGroupToUsers
                .Where(u => u.UserId == creatingUserId && u.GroupId == groupId)
                .FirstOrDefaultAsync();

        if (creatingUserUserGroup == null)
            throw new InvalidOperationException("The creating user doesn't exist in the group from where the member is being removed.");
        var adminRole = await _applicationDbContext.AnaRoles
                .Where(r => r.Name == AnaRoleNames.Admin)
                .FirstOrDefaultAsync();
        if (adminRole == null)
            throw new InvalidOperationException("The Admin role was not found in the system");
        if (creatingUserUserGroup.RoleId != adminRole.Id)
            throw new InvalidOperationException("The user removing the member is not an admin of the group");


        var groupToUser = await _applicationDbContext.AnaGroupToUsers
            .FirstOrDefaultAsync(agu => agu.UserId == userId && agu.GroupId == groupId);

        if (groupToUser == null)
        {
            _logger.LogWarning("User {userId} isn't present in group {groupId}", userId, groupId);
            throw new InvalidOperationException($"User {userId} is not present in group {groupId}");
        }

        _applicationDbContext.AnaGroupToUsers.Remove(groupToUser);
        await _applicationDbContext.SaveChangesAsync();
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

    public async Task<AnaUser> GetUserSettings(string userId)
    {
        _logger.LogInformation("Getting user settings for user {userId}", userId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var userSettings = await _applicationDbContext.AnaUsers
            .Where(agu => agu.Id == userId)
            .FirstOrDefaultAsync();
        if (userSettings == null)
        {
            userSettings = new AnaUser
            {
                Id = userId,
                DisplayName = string.Empty,
                SelectedGroupId = string.Empty,
                PreferredNotification = PreferredNotifications.None,
                WhatsAppNumber = string.Empty
            };
        }
        return userSettings;
    }

    public async Task UpdateUserSettings(string userId, AnaUser userSettings)
    {
        _logger.LogInformation("Updating user settings for user {userId}", userId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var idUser = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (idUser == null)
        {
            _logger.LogError("User with ID {userId} not found", userId);
            throw new InvalidOperationException($"User with ID {userId} not found");
        }
        idUser.UserName = userSettings.DisplayName;
        _applicationDbContext.Users.Update(idUser);



        var existingUserSettings = await _applicationDbContext.AnaUsers
            .Where(agu => agu.Id == userId)
            .FirstOrDefaultAsync();

        if (existingUserSettings == null)
        {
            throw new InvalidOperationException("The user already exists");
        }

        existingUserSettings.DisplayName = userSettings.DisplayName;
        existingUserSettings.SelectedGroupId = userSettings.SelectedGroupId;
        existingUserSettings.PreferredNotification = userSettings.PreferredNotification;
        existingUserSettings.WhatsAppNumber = userSettings.WhatsAppNumber;

        _applicationDbContext.AnaUsers.Update(existingUserSettings);
        await _applicationDbContext.SaveChangesAsync();
    }

    public async Task DeleteUser(string userId)
    {
        _logger.LogInformation("Deleting user {userId}", userId);

        var _applicationDbContext = _dbContextFactory.CreateDbContext();

        var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogError("User with ID {userId} not found", userId);
            throw new InvalidOperationException($"User with ID {userId} not found");
        }
        _applicationDbContext.Users.Remove(user);
        var anaUser = await _applicationDbContext.AnaUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (anaUser == null)
        {
            _logger.LogError("AnaUser with ID {userId} not found", userId);
            throw new InvalidOperationException($"AnaUser with ID {userId} not found");
        }
        _applicationDbContext.AnaUsers.Remove(anaUser);

        // Will remove groups where I am last user. For removed groups, remove their anniversaries first.
        var groupToUsers = await _applicationDbContext.AnaGroupToUsers
            .Where(gu => gu.UserId == anaUser.Id).ToListAsync();
        var groupsToUsersRemove = new List<AnaGroupToUser>();
        foreach (var gtu in groupToUsers)
        {
            var otherGroupMembersCount = await _applicationDbContext.AnaGroupToUsers
            .Where(gu => gu.GroupId == gtu.GroupId && gu.UserId != anaUser.Id)
            .CountAsync();
            if (otherGroupMembersCount == 0)
            {
                groupsToUsersRemove.Add(gtu);
            }
        }
        foreach (var gtur in groupsToUsersRemove)
        {
            var atr = await _applicationDbContext.AnaAnnivs
                .Where(an => an.GroupId == gtur.GroupId)
                .ToListAsync();
            _logger.LogInformation($"Removing Anniversaries for cancelled user [{string.Join(",", atr.Select(a => a.Id))}]");
            _applicationDbContext.AnaAnnivs.RemoveRange(atr.ToArray());
        }
        _logger.LogInformation($"Removing AnaGroupToUsers for cancelled user [{string.Join(",", groupsToUsersRemove.Select(a => a.GroupId + "-" + a.UserId))}]");
        _applicationDbContext.AnaGroupToUsers.RemoveRange(groupsToUsersRemove.ToArray());

        var groupsToRemoveList = groupsToUsersRemove.Select(gtu => gtu.GroupId).Distinct();
        _logger.LogInformation($"Removing AnaGroups for cancelled user [{string.Join(",", groupsToRemoveList)}]");
        var groupsToRemove = await _applicationDbContext.AnaGroups
                .Where(g => groupsToRemoveList.Contains(g.Id))
                .ToListAsync();

        _applicationDbContext.AnaGroups.RemoveRange(groupsToRemove.ToArray());

        await _applicationDbContext.SaveChangesAsync();
    }

    public async Task DailyTask()
    {
        _logger.LogInformation("Starting daily task");
        await _dailyTaskService.RunNowAsync();
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
