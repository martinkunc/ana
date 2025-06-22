
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

        ConfigureIdentityForCosmosDb(builder);
    }

    private void ConfigureIdentityForCosmosDb(ModelBuilder builder)
    {
        var anaGroupBuilder = builder.Entity<AnaGroup>()
            .ToContainer("AnaGroups");

        anaGroupBuilder.HasKey(g => g.Id);
        anaGroupBuilder
            //.HasPartitionKey(g => g.Id)
            .UseETagConcurrency()
            .HasNoDiscriminator();

        var anaRoleBuilder = builder.Entity<AnaRole>()
            .ToContainer("AnaRoles");
        anaRoleBuilder.HasKey(r => r.Id);
        anaRoleBuilder
            .UseETagConcurrency()
            .HasNoDiscriminator();


        var groupToUsersBuilder = builder.Entity<AnaGroupToUser>()
            .ToContainer("AnaGroupToUsers")
            .HasPartitionKey(g => g.UserId);
        groupToUsersBuilder.HasKey(r => new { r.UserId, r.GroupId });
        groupToUsersBuilder
            .UseETagConcurrency()
            .HasNoDiscriminator();

        var anaAnnivBuilder = builder.Entity<AnaAnniv>()
            .ToContainer("AnaAnnivs");
        anaAnnivBuilder.HasKey(r => r.Id);
        anaAnnivBuilder
            .UseETagConcurrency()
            .HasNoDiscriminator();

        var anaUserBuilder = builder.Entity<AnaUser>()
            .ToContainer("AnaUsers");
        anaUserBuilder.HasKey(r => r.Id);
        anaUserBuilder
            .UseETagConcurrency()
            .HasNoDiscriminator();
    }

    public virtual DbSet<AnaGroup> AnaGroups { get; set; } = default!;

    public virtual DbSet<AnaGroupToUser> AnaGroupToUsers { get; set; } = default!;

    public virtual DbSet<AnaRole> AnaRoles { get; set; } = default!;

    public virtual DbSet<AnaAnniv> AnaAnnivs { get; set; } = default!;
    
    public virtual DbSet<AnaUser> AnaUsers { get; set; } = default!;
    
}