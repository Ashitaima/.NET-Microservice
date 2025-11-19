using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ArtAuction.Domain.Common;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; protected set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    [BsonElement("version")]
    public int Version { get; protected set; } = 1;

    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
