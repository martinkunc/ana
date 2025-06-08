using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure for Cosmos DB
        builder.HasDefaultContainer("Identity");
        
        // Configure partition keys
        builder.Entity<IdentityUser>().HasPartitionKey(u => u.Id);
        builder.Entity<IdentityRole>().HasPartitionKey(r => r.Id);
        builder.Entity<IdentityUserRole<string>>().HasPartitionKey(ur => ur.UserId);
        builder.Entity<IdentityUserClaim<string>>().HasPartitionKey(uc => uc.UserId);
        builder.Entity<IdentityUserLogin<string>>().HasPartitionKey(ul => ul.UserId);
        builder.Entity<IdentityUserToken<string>>().HasPartitionKey(ut => ut.UserId);
        builder.Entity<IdentityRoleClaim<string>>().HasPartitionKey(rc => rc.RoleId);
    }
}