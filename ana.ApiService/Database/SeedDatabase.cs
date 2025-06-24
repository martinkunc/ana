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

        await cosmosClient.CreateDatabaseIfNotExistsAsync(Config.Database.Name);

        var database = cosmosClient
            .GetDatabase(Config.Database.Name);

        //await context.Database.EnsureCreatedAsync();

        //var container = await database.CreateContainerIfNotExistsAsync("Identity","/id");


        var containerNames = new[]
        {
            "Identity", "Identity_Logins", "Identity_DeviceFlowCodes", "Identity_PersistedGrant",
            "Identity_Roles",  "Identity_UserRoles","Identity_Tokens", "AnaGroups", "AnaGroupToUsers",
            "AnaRoles", "AnaAnnivs", "AnaUsers"
        };
        var keys = new[] {
            "Id","ProviderKey","SessionId","Key",
            "Id","UserId","UserId", "Id", "UserId",
            "Id", "Id","Id"
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

        if (await context.Set<IdentityUser>().FirstOrDefaultAsync() == null)
        {
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
        }

        if (await context.Set<IdentityUserLogin<string>>().FirstOrDefaultAsync() == null)
        {
            context.UserLogins.AddRange(
               new IdentityUserLogin<string>
               {
                   UserId = "admin",
                   LoginProvider = "CustomLoginProvider",
                   ProviderKey = "CustomProviderKey",
                   ProviderDisplayName = "CustomProvider"

               });
            await context.SaveChangesAsync();
        }

        if (await context.Set<IdentityRole>().FirstOrDefaultAsync() == null)
        {
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
        }

        if (await context.Set<IdentityUserRole<string>>().FirstOrDefaultAsync() == null)
        {
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
        }

        if (await context.Set<AnaRole>().FirstOrDefaultAsync() == null)
        {
                context.AnaRoles.AddRange(
                    new AnaRole
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = Config.Roles.Admin,
                    }, new AnaRole
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = Config.Roles.Admin,
                    });
                await context.SaveChangesAsync();
        }


        if (await context.Set<AnaGroup>().FirstOrDefaultAsync() == null)
        {
            // var adminRole = await context.AnaGroups.FirstOrDefaultAsync(r => r.Name == "Admin");
            // var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
            var adminRole = await context.AnaRoles.FirstOrDefaultAsync(u => u.Name == Config.Roles.Admin);
            if (adminUser != null && adminRole != null)
            {
                var adminsGroup = new AnaGroup
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Admin's group",
                };
                context.AnaGroups.Add(adminsGroup);
                await context.SaveChangesAsync();

                var adminsGroupToUser = await context.AnaGroupToUsers
                    .FirstOrDefaultAsync(
                        u => u.UserId == adminUser.Id && u.GroupId == adminsGroup.Id);
                if (adminsGroupToUser == null)
                {
                    context.AnaGroupToUsers.Add(new AnaGroupToUser
                    {
                        UserId = adminUser.Id,
                        GroupId = adminsGroup.Id,
                        RoleId = adminRole.Id,
                    });
                    await context.SaveChangesAsync();
                }
            }
        }

        // if (await context.Set<AnaGroupToUser>().FirstOrDefaultAsync() == null)
        // {
        //     // var adminRole = await context.AnaGroups.FirstOrDefaultAsync(r => r.Name == "Admin");
        //     // var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        //     var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
        //     var adminsGroup = await context.AnaGroups.FirstOrDefaultAsync(u => u. == "admin");
        //     if (adminUser != null)
        //     {
        //         var adminsGroup = new AnaGroup
        //         {
        //             Id = Guid.NewGuid().ToString(),
        //             Name = "Admin's group",
        //         };
        //         context.AnaGroups.Add(adminsGroup);

        //         context.AnaGroupToUsers.Add(new AnaGroupToUser
        //         {
        //             UserId = adminUser.Id,
        //             GroupId = adminsGroup.Id
        //         });
        //     }
        //     await context.SaveChangesAsync();

        // }

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