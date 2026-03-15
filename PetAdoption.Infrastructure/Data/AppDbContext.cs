using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Entities;

namespace PetAdoption.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Shelter> Shelters => Set<Shelter>();
        public DbSet<Adopter> Adopters => Set<Adopter>();
        public DbSet<Pet> Pets => Set<Pet>();
        public DbSet<AdoptionRequest> AdoptionRequests => Set<AdoptionRequest>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Shelter>(e =>
            {
                e.Property(p => p.Name).HasMaxLength(128).IsRequired();
                e.HasMany(s => s.Pets).WithOne(p => p.Shelter).HasForeignKey(p => p.ShelterId).OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<Adopter>(e => {
                e.Property(a => a.FullName).HasMaxLength(128).IsRequired();
            });

            b.Entity<Pet>(e => {
                e.Property(p => p.Name).HasMaxLength(128).IsRequired();
                e.Property(p => p.Breed).HasMaxLength(128);
                e.Property(p => p.ImageUrl).HasMaxLength(512);
            });

            b.Entity<AdoptionRequest>(e =>
            {
                e.HasOne(r => r.Pet).WithMany().HasForeignKey(r => r.PetId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Adopter).WithMany().HasForeignKey(r => r.AdopterId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}