using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualBasic;

public static class SeedDatabase
{


    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<IdentityUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        string? defaultAdminPassword = await GetDefaultAdminPassword(configuration);

        //await cosmosClient.CreateDatabaseIfNotExistsAsync("IdentityDatabase");

        //await context.Database.EnsureCreatedAsync();
        Console.WriteLine("Creating Cosmos DB database and containers...");

        var database = cosmosClient
            .GetDatabase(Config.Database.Name);

        //await context.Database.EnsureCreatedAsync();

        //var container = await database.CreateContainerIfNotExistsAsync("Identity","/id");


        var containerNames = new[]
        {
            "Identity", "Identity_Logins", "Identity_DeviceFlowCodes", "Identity_PersistedGrant",
            "Identity_Roles",  "Identity_UserRoles","Identity_Tokens"
        };
        var keys = new[] {
            "Id","ProviderKey","SessionId","Key",
            "Id","UserId","UserId"
        };
        for (var i = 0; i < containerNames.Length; i++)
        {
            var containerName = containerNames[i];
            var key = keys[i];
            ContainerProperties containerProperties = new ContainerProperties(
                id: containerName,
                partitionKeyPath: "/" + key
            );



            // Set the partition key kind to Hash
            //containerProperties.PartitionKeyDefinition.Kind = PartitionKeyKind.Hash;
            //containerProperties.PartitionKeyDefinitionVersion = Cosmos.PartitionKeyDefinitionVersion.V1;
            bool exists = await DoesContainerExist(database, containerName);

            if (exists)
                continue;

            var container = await database.CreateContainerAsync(containerProperties);

        }

        await PopulateContainer(context, passwordHasher, defaultAdminPassword);

        //await context.Database.EnsureCreatedAsync();

    }

    private static async Task<string> GetDefaultAdminPassword(IConfiguration configuration)
    {
        var defaultAdminPassword = configuration["DefaultAdminPassword"];
        var defaultAdminPasswordIsEmpty = configuration["DefaultAdminPasswordIsEmpty"];
        if (defaultAdminPasswordIsEmpty == true.ToString())
        {
            defaultAdminPassword = "";
        }
        if (defaultAdminPassword == null)
        {
            var client = new SecretClient(new Uri(Config.KeyVault.KeyVaultUrl), new DefaultAzureCredential());
            KeyVaultSecret secret = await client.GetSecretAsync(Config.Users.DefaultAdminPasswordKeyVaultSecretName);
            defaultAdminPassword = secret.Value;
        }
        if (defaultAdminPassword == null)
        {
            defaultAdminPassword = "";
        }

        return defaultAdminPassword;
    }

    private static async Task<bool> DoesContainerExist(Database database, string containerName)
    {
        bool exists = false;
        try
        {
            Container existingContainer = database.GetContainer(containerName);
            await existingContainer.ReadContainerAsync();
            exists = true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }

        return exists;
    }

    private static async Task PopulateContainer(ApplicationDbContext context, IPasswordHasher<IdentityUser> passwordHasher, string defaultAdminPassword)
    {
        var et = context.Model.GetEntityTypes();

        foreach (var entityType in et)
        {
            switch (entityType.ClrType)
            {
                case Type t when t == typeof(IdentityUser):
                    if (await context.Set<IdentityUser>().FirstOrDefaultAsync() != null)
                        break;
                    context.Users.AddRange(
                       new IdentityUser
                       {
                           UserName = "admin",
                           Email = "admin@mail.com",
                           EmailConfirmed = true,
                           PasswordHash = passwordHasher.HashPassword(null, defaultAdminPassword),
                           SecurityStamp = Guid.NewGuid().ToString(),
                           NormalizedUserName = "ADMIN",
                           NormalizedEmail = "admin@mail.com".ToUpperInvariant(),
                           PhoneNumber = "1234567890",
                           PhoneNumberConfirmed = true,
                           TwoFactorEnabled = false,
                           LockoutEnabled = false,
                           AccessFailedCount = 0
                       });
                    await context.SaveChangesAsync();
                    break;
                case Type t when t == typeof(IdentityUserLogin<string>):
                    if (await context.Set<IdentityUserLogin<string>>().FirstOrDefaultAsync() != null)
                        break;
                    context.UserLogins.AddRange(
                       new IdentityUserLogin<string>
                       {
                           UserId = "admin",
                           LoginProvider = "CustomLoginProvider",
                           ProviderKey = "CustomProviderKey",
                           ProviderDisplayName = "CustomProvider"
                       
                       });
                    await context.SaveChangesAsync();
                    break;
                case Type t when t == typeof(IdentityRole): // is this entity needed
                    if (await context.Set<IdentityRole>().FirstOrDefaultAsync() != null)
                        break;
                    context.Roles.AddRange(
                        new IdentityRole
                        {
                            Name = "Admin",
                            NormalizedName = "ADMIN"
                        },
                        new IdentityRole
                        {
                            Name = "User",
                            NormalizedName = "USER"
                        });
                    await context.SaveChangesAsync();
                    break;
                case Type t when t == typeof(IdentityUserRole<string>):
                
                    if (await context.Set<IdentityUserRole<string>>().FirstOrDefaultAsync() != null)
                        break;

                    var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                    var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                    var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
                    if (adminRole != null && adminUser != null)
                    {
                        context.UserRoles.Add(new IdentityUserRole<string>
                        {
                            UserId = adminUser.Id,
                            RoleId = adminRole.Id
                        });
                    }
                    await context.SaveChangesAsync();
                    break;
                    // case "UserClaims":
                    //     var adminUserClaim = context.Users.FirstOrDefault(u => u.UserName == "admin");
                    //     if (adminUserClaim != null)
                    //     {
                    //         context.UserClaims.Add(new IdentityUserClaim<string>()
                    //         {
                    //             UserId = adminUserClaim.Id,
                    //             ClaimType = "AdminClaim",
                    //             ClaimValue = "true"
                    //         });
                    //     }
                    //     await context.SaveChangesAsync();
                    //     break;
                    // case "UserLogins":
                    //     var adminUserLogin = context.Users.FirstOrDefault(u => u.UserName == "admin");
                    //     if (adminUserLogin != null)
                    //     {
                    //         context.UserLogins.Add(new IdentityUserLogin<string>
                    //         {
                    //             UserId = adminUserLogin.Id,
                    //             LoginProvider = "CustomLoginProvider",
                    //             ProviderKey = "CustomProviderKey",
                    //             ProviderDisplayName = "CustomProvider"
                    //         });
                    //     }
                    //     await context.SaveChangesAsync();
                    //     break;
                    // case "UserTokens":
                    //     var adminUserToken = context.Users.FirstOrDefault(u => u.UserName == "admin");
                    //     if (adminUserToken != null)
                    //     {
                    //         context.UserTokens.Add(new IdentityUserToken<string>
                    //         {
                    //             UserId = adminUserToken.Id,
                    //             LoginProvider = "CustomLoginProvider",
                    //             Name = "CustomTokenName",
                    //             Value = "CustomTokenValue"
                    //         });
                    //     }
                    //     await context.SaveChangesAsync();
                    //     break;
                    // case "RoleClaims":
                    //     var adminRoleClaim = context.Roles.FirstOrDefault(r => r.Name == "Admin");
                    //     if (adminRoleClaim != null)
                    //     {
                    //         context.RoleClaims.Add(new IdentityRoleClaim<string>
                    //         {
                    //             RoleId = adminRoleClaim.Id,
                    //             ClaimType = "AdminClaim",
                    //             ClaimValue = "true"
                    //         });
                    //     }
                    //     await context.SaveChangesAsync();
                    //     break;

                    // default:
                    //     throw new ArgumentException($"Unknown container name: {containerName}", nameof(containerName));

            }
        }
    }



    // // Seed data if necessary
    // if (!context.WeatherForecasts.Any())
    // {
    //     context.WeatherForecasts.AddRange(
    //         Enumerable.Range(1, 5).Select(index => new WeatherForecast
    //         {
    //             Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
    //             TemperatureC = Random.Shared.Next(-20, 55),
    //             Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    //         })
    //     );
    //     context.SaveChanges();
    // }
    //     }
}