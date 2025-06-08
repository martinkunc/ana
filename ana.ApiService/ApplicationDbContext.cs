
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : CosmosIdentityDbContext<IdentityUser, IdentityRole, string>

{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        //ConfigureIdentityForCosmosDb(builder);
    }

    private void ConfigureIdentityForCosmosDb(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // Remove all indexes
            foreach (var index in entityType.GetIndexes().ToList())
            {
                entityType.RemoveIndex(index);
            }
        }

        // builder.Entity<ApplicationUser>()
        //     .ToContainer("Users")
        //     .HasPartitionKey(u => u.Id)
        //     .UseETagConcurrency()
        //     .HasNoDiscriminator()
        //     .Ignore(u => u.ConcurrencyStamp);

        // builder.Entity<IdentityRole>()
        //     .ToContainer("Roles")
        //     .HasPartitionKey(r => r.Id)
        //     .UseETagConcurrency()
        //     .HasNoDiscriminator()
        //     .Ignore(r => r.ConcurrencyStamp);

        // builder.Entity<IdentityUserRole<string>>()
        //     .ToContainer("UserRoles")
        //     .HasPartitionKey(ur => ur.UserId)
        //     .HasNoDiscriminator();

        // builder.Entity<IdentityUserClaim<string>>()
        //     .ToContainer("UserClaims")
        //     .HasPartitionKey(uc => uc.UserId)
        //     .HasNoDiscriminator();

        // builder.Entity<IdentityUserLogin<string>>()
        //     .ToContainer("UserLogins")
        //     .HasPartitionKey(ul => ul.UserId)
        //     .HasNoDiscriminator();

        // builder.Entity<IdentityUserToken<string>>()
        //     .ToContainer("UserTokens")
        //     .HasPartitionKey(ut => ut.UserId)
        //     .HasNoDiscriminator();

        // builder.Entity<IdentityRoleClaim<string>>()
        //     .ToContainer("RoleClaims")
        //     .HasPartitionKey(rc => rc.RoleId)
        //     .HasNoDiscriminator();

        // builder.Entity<ApplicationUser>()
        //     .ToContainer("Identity")
        //     .HasPartitionKey(u => u.Id)
        //     .UseETagConcurrency()
        //     .Ignore(r => r.ConcurrencyStamp)
        //     .Property(u => u.Id)
        //         .ToJsonProperty("id");

        // builder.Entity<ApplicationUser>()
        //     .UseETagConcurrency()
        //     .Ignore(r => r.ConcurrencyStamp)
        //     .HasDiscriminator<string>("entityType")
        //     .HasValue<ApplicationUser>("User");

        // builder.Entity<IdentityRole>()
        //     .ToContainer("Identity")
        //     .HasPartitionKey(r => r.Id)
        //     .UseETagConcurrency()
        //     .Ignore(r => r.ConcurrencyStamp)
        //     .Property(r => r.Id)
        //         .ToJsonProperty("id");

        // builder.Entity<IdentityRole>()
        //     .HasDiscriminator<string>("entityType")
        //     .HasValue<IdentityRole>("Role");


        // var roleBuilder = builder.Entity<IdentityUserRole<string>>()
        //     .ToContainer("Identity")
        //     .HasPartitionKey(ur => ur.RoleId);

        // roleBuilder.Property(ur => ur.RoleId)
        //     .ToJsonProperty("id");

        // roleBuilder.Property(ur => ur.UserId)
        //     .ToJsonProperty("UserId");

        // // builder.Entity<IdentityUserRole<string>>()
        // //     .ToContainer("Identity")
        // //     .HasPartitionKey(ur => ur.RoleId)
        // //     .Property(ur => ur.UserId)
        // //     .ToJsonProperty("UserId");

        // builder.Entity<IdentityUserRole<string>>()
        //     .HasDiscriminator<string>("entityType")
        //     .HasValue("UserRole");
        // builder.Entity<IdentityUserClaim<string>>()
        //     .ToContainer("Identity")
        //     .HasPartitionKey(uc => uc.UserId)
        //     .Property(uc => uc.UserId)
        //     .ToJsonProperty("id");
        // builder.Entity<IdentityUserClaim<string>>()
        //     .HasDiscriminator<string>("entityType")
        //     .HasValue("UserClaim");
        // builder.Entity<IdentityUserLogin<string>>()
        //     .ToContainer("Identity")
        //     .HasPartitionKey(ul => ul.UserId)
        //     .Property(ul => ul.UserId)
        //     .ToJsonProperty("id");
        // builder.Entity<IdentityUserLogin<string>>()
        //     .HasDiscriminator<string>("entityType")
        //     .HasValue("UserLogin");
        // builder.Entity<IdentityUserToken<string>>()
        //     .ToContainer("Identity")
        //     .HasPartitionKey(ut => ut.UserId)
        //     .Property(ut => ut.UserId)
        //     .ToJsonProperty("id");
        // builder.Entity<IdentityUserToken<string>>()
        //     .HasDiscriminator<string>("entityType")
        //     .HasValue("UserToken");
        // builder.Entity<IdentityRoleClaim<string>>()
        //     .ToContainer("Identity")
        //     .HasPartitionKey(rc => rc.RoleId)
        //     .Property(rc => rc.RoleId)
        //     .ToJsonProperty("id");
        // builder.Entity<IdentityRoleClaim<string>>()
        //     .HasDiscriminator<string>("entityType")
        //     .HasValue("RoleClaim");
        
        
    }
}