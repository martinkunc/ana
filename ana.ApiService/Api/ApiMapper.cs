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

        authGroup.MapGet("user/groups/{userId}", apiEndpoints.GetUserGroups)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces<GetUserGroupsResponse>();

        authGroup.MapGet("user/select-group/{userId}", apiEndpoints.GetSelectedGroup)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        authGroup.MapPost("user/select-group/{userId}/{groupId}", apiEndpoints.SelectGroup)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status200OK);

        // api/v1/group/{encodedGroupId}/anniversaries
        authGroup.MapGet("group/{groupId}/anniversaries", apiEndpoints.GetAnniversaries)
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

        // group.MapGet("items/type/all/brand/{catalogBrandId:int}", (int catalogBrandId, CatalogDbContext catalogContext, int? before, int? after, int pageSize = 8)
        //     => GetCatalogItems(catalogBrandId, catalogContext, before, after, pageSize))
        //     .Produces(StatusCodes.Status400BadRequest)
        //     .Produces<CatalogItemsPage>();

        // static async Task<IResult> GetCatalogItems(int? catalogBrandId, CatalogDbContext catalogContext, int? before, int? after, int pageSize)
        // {
        //     if (before is > 0 && after is > 0)
        //     {
        //         return TypedResults.BadRequest($"Invalid paging parameters. Only one of {nameof(before)} or {nameof(after)} can be specified, not both.");
        //     }

        //     var itemsOnPage = await catalogContext.GetCatalogItemsCompiledAsync(catalogBrandId, before, after, pageSize);

        //     var (firstId, nextId) = itemsOnPage switch
        //     {
        //         [] => (0, 0),
        //         [var only] => (only.Id, only.Id),
        //         [var first, .., var last] => (first.Id, last.Id)
        //     };

        //     return Results.Ok(new CatalogItemsPage(
        //         firstId,
        //         nextId,
        //         itemsOnPage.Count < pageSize,
        //         itemsOnPage.Take(pageSize)));
        // }

        // group.MapGet("items/{catalogItemId:int}/image", async (int catalogItemId, CatalogDbContext catalogDbContext, IHostEnvironment environment) =>
        // {
        //     var item = await catalogDbContext.CatalogItems.FindAsync(catalogItemId);

        //     if (item is null)
        //     {
        //         return Results.NotFound();
        //     }

        //     var path = Path.Combine(environment.ContentRootPath, "Images", item.PictureFileName);

        //     if (!File.Exists(path))
        //     {
        //         return Results.NotFound();
        //     }

        //     return Results.File(path, "image/jpeg");
        // })
        // .Produces(404)
        // .Produces(200, contentType: "image/jpeg");

        return group;
    }

    

}