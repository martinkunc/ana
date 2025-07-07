using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

public static class ApiMapper {

    public static IApiEndpoints apiEndpoints;


    public static RouteGroupBuilder MapApiEndpoints(this IEndpointRouteBuilder routes)
    {
        //var rootGroup = routes.MapGroup("/");
        //rootGroup.MapGet("/", () => "API is running");
        apiEndpoints = routes.ServiceProvider.GetService<IApiEndpoints>() ?? throw new InvalidOperationException("IApiEndpoints service is not registered.");



        var group = routes.MapGroup("/api/v1/");

        group.WithTags("AnaTag");



        var authGroup = routes.MapGroup("/api/v1/")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            });


        authGroup.MapPost("group", apiEndpoints.CreateGroup)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces<CreateGroupResponse>();

        authGroup.MapPost("user", apiEndpoints.CreateUser)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        authGroup.MapGet("user/groups/{userId}", apiEndpoints.GetUserGroups)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces<GetUserGroupsResponse>();

        authGroup.MapGet("user/select-group/{userId}", apiEndpoints.GetSelectedGroup)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        authGroup.MapPost("user/select-group/{userId}/{groupId}", apiEndpoints.SelectGroup)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);
        
        authGroup.MapGet("group/{groupId}/members", apiEndpoints.GetGroupMembers)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        // api/v1/group/{encodedGroupId}/anniversaries
        authGroup.MapGet("group/{groupId}/anniversaries", apiEndpoints.GetAnniversaries)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        // api/v1/group/{encodedGroupId}/member
        authGroup.MapPost("group/{groupId}/member", apiEndpoints.CreateGroupMember)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        // api/v1/group/{encodedGroupId}/member/{encodedUserId}/role
        authGroup.MapPut("group/{groupId}/member/{userId}/role", apiEndpoints.ChangeGroupMemberRole)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);
        
        // api/v1/group/{encodedGroupId}/member/{encodedUserId}
        authGroup.MapDelete("group/{groupId}/member/{userId}", apiEndpoints.DeleteGroupMember)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        // api/v1/group/{encodedGroupId}/anniversaries
        authGroup.MapPost("group/{groupId}/anniversary", apiEndpoints.CreateAnniversary)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        authGroup.MapPut("group/{groupId}/anniversary/{anniversaryId}", apiEndpoints.UpdateAnniversary)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);


        authGroup.MapDelete("group/{groupId}/anniversary/{anniversaryId}", apiEndpoints.DeleteAnniversary)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        authGroup.MapGet("user/{userId}", apiEndpoints.GetUserSettings)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        authGroup.MapPut("user/{userId}", apiEndpoints.UpdateUserSettings)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        // api/v1/user/{encodedUserId}
        authGroup.MapDelete("user/{userId}", apiEndpoints.DeleteUser)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        // api/v1/daily-task
        authGroup.MapPost("daily-task", apiEndpoints.DailyTask)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);


        return group;
    }

    

}