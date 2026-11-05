using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Shelter> Shelters => Set<Shelter>();

        public DbSet<Adopter> Adopters => Set<Adopter>();

        public DbSet<Pet> Pets => Set<Pet>();

        public DbSet<AdoptionRequest> AdoptionRequests =>
            Set<AdoptionRequest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Shelter>(entity =>
            {
                entity.Property(shelter => shelter.Name)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.HasMany(shelter => shelter.Pets)
                    .WithOne(pet => pet.Shelter)
                    .HasForeignKey(pet => pet.ShelterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Adopter>(entity =>
            {
                entity.Property(adopter => adopter.FullName)
                    .HasMaxLength(128)
                    .IsRequired();
            });

            modelBuilder.Entity<Pet>(entity =>
            {
                entity.Property(pet => pet.Name)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(pet => pet.Breed)
                    .HasMaxLength(128);

                entity.Property(pet => pet.ImageUrl)
                    .HasMaxLength(512);
            });

            modelBuilder.Entity<AdoptionRequest>(entity =>
            {
                entity.HasOne(request => request.Pet)
                    .WithMany()
                    .HasForeignKey(request => request.PetId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(request => request.Adopter)
                    .WithMany()
                    .HasForeignKey(request => request.AdopterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(request => request.PetId)
                    .IsUnique()
                    .HasFilter(
                        $"\"Status\" = {(int)AdoptionRequestStatus.Pending}"
                    );
            });
        }
    }
}