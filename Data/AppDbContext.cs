using GroupsMicroservice.Models.DB;
using Microsoft.EntityFrameworkCore;

namespace GroupsMicroservice.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMembers> GroupMembers => Set<GroupMembers>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.UserName)
                .IsRequired();

            entity.Property(u => u.FirstName)
                .IsRequired();

            entity.Property(u => u.LastName)
                .IsRequired();

            entity.Property(u => u.Email)
                .IsRequired();
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);

            entity.Property(g => g.Name)
                .IsRequired();

            entity.Property(g => g.Description)
                .IsRequired();

            entity.HasOne(g => g.CreatedByUser)
                .WithMany(u => u.CreatedGroups)
                .HasForeignKey(g => g.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupMembers>(entity =>
        {
            entity.HasKey(gm => gm.Id);

            entity.HasOne(gm => gm.User)
                .WithMany(u => u.GroupMembers)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gm => gm.Group)
                .WithMany(g => g.GroupMembers)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(gm => new { gm.UserId, gm.GroupId })
                .IsUnique();
        });
    }
}