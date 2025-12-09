using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rezarwacja_Sal.Models;

namespace Rezarwacja_Sal.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<Attachment> Attachments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

          
            builder.Entity<Room>(entity =>
            {
                entity.ToTable("Rooms");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
                entity.Property(r => r.Capacity).IsRequired();
                entity.Property(r => r.Location).HasMaxLength(300);
                entity.Property(r => r.Equipment).HasMaxLength(2000);
                entity.Property(r => r.IsActive).HasDefaultValue(true);

               
                entity.HasIndex(r => r.Name).IsUnique(false);
               
                entity.HasIndex(r => new { r.Location, r.Name }).IsUnique();
            });

           
            builder.Entity<Reservation>(entity =>
            {
                entity.ToTable("Reservations", t =>
                {
                  
                    t.HasCheckConstraint("CK_Reservations_StartBeforeEnd", "[StartAt] < [EndAt]");
                });
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
                entity.Property(r => r.Notes).HasMaxLength(4000);
                entity.Property(r => r.CreatedByUserId).HasMaxLength(450);
                entity.Property(r => r.Status)
                      .HasConversion<int>()
                      .HasDefaultValue(ReservationStatus.Pending);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(r => r.Room)
                      .WithMany()
                      .HasForeignKey(r => r.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

              
                entity.HasIndex(r => new { r.RoomId, r.StartAt, r.EndAt });
            });

           
            builder.Entity<Attachment>(entity =>
            {
                entity.ToTable("Attachments");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.OriginalFileName).IsRequired().HasMaxLength(260);
                entity.Property(a => a.StoredFileName).IsRequired().HasMaxLength(260);
                entity.Property(a => a.ContentType).IsRequired().HasMaxLength(200);
                entity.Property(a => a.RelativePath).HasMaxLength(500);
                entity.Property(a => a.UploadedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(a => a.ReservationId);
            });
        }
    }
}
