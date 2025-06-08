using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

public static class AnaApi
{
    private static readonly string[] Summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

    public static RouteGroupBuilder MapAnaApi(this IEndpointRouteBuilder routes)
    {
        //var rootGroup = routes.MapGroup("/");
        //rootGroup.MapGet("/", () => "API is running");

            

        var group = routes.MapGroup("/api/v1/");

        group.WithTags("AnaTag");

        group.MapGet("weatherforecast", () =>
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        })
            .Produces(StatusCodes.Status400BadRequest)
            .Produces<WeatherForecast>();

        var authGroup = routes.MapGroup("/api/v1/")
                        .RequireAuthorization(new AuthorizeAttribute
                        {
                            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
                        });

        authGroup.MapGet("authweatherforecast", () =>
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        })
            .Produces(StatusCodes.Status400BadRequest)
            .Produces<WeatherForecast>();



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