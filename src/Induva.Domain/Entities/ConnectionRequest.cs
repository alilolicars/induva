using Induva.Domain.Common;
using Induva.Domain.Enums;

namespace Induva.Domain.Entities;

public class ConnectionRequest : BaseEntity
{
    public string FromUserId { get; set; } = default!;
    public string ToUserId { get; set; } = default!;
    public MatchEntityType RelatedType { get; set; }
    public int RelatedId { get; set; }
    public string Message { get; set; } = "";
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
    public DateTime? RespondedAtUtc { get; set; }
}

