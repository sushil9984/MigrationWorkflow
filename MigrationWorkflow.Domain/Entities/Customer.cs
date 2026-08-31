using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MigrationWorkflow.Domain.Entities;

/// <summary>
/// PostgreSQL Customer entity
/// </summary>
[Table("customers")]
public class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    [Column("customer_id")]
    public string CustomerId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    [Column("customer_type")]
    public string CustomerType { get; set; } = string.Empty;
    
    [MaxLength(100)]
    [Column("email")]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }
    
    // Address fields (denormalized)
    [MaxLength(200)]
    [Column("street")]
    public string? Street { get; set; }
    
    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }
    
    [MaxLength(50)]
    [Column("state")]
    public string? State { get; set; }
    
    [MaxLength(20)]
    [Column("zip_code")]
    public string? ZipCode { get; set; }
    
    [MaxLength(100)]
    [Column("country")]
    public string? Country { get; set; }
    
    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "Active";
    
    [Column("created_date")]
    public DateTime CreatedDate { get; set; }
    
    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }
    
    [Column("source_id")]
    [MaxLength(50)]
    public string? SourceId { get; set; } // Original MongoDB _id
    
    [Column("migrated_at")]
    public DateTime? MigratedAt { get; set; }
}
