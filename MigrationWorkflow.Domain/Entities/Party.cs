using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MigrationWorkflow.Domain.Entities;

/// <summary>
/// MongoDB Party document
/// </summary>
public class Party
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("partyId")]
    public string PartyId { get; set; } = string.Empty;
    
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty; // Individual, Organization
    
    [BsonElement("email")]
    public string? Email { get; set; }
    
    [BsonElement("phone")]
    public string? Phone { get; set; }
    
    [BsonElement("address")]
    public Address? Address { get; set; }
    
    [BsonElement("status")]
    public string Status { get; set; } = "Active";
    
    [BsonElement("createdDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedDate { get; set; }
    
    [BsonElement("modifiedDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ModifiedDate { get; set; }
    
    [BsonElement("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public class Address
{
    [BsonElement("street")]
    public string? Street { get; set; }
    
    [BsonElement("city")]
    public string? City { get; set; }
    
    [BsonElement("state")]
    public string? State { get; set; }
    
    [BsonElement("zipCode")]
    public string? ZipCode { get; set; }
    
    [BsonElement("country")]
    public string? Country { get; set; }
}
