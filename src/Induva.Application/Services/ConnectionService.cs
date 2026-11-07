using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Domain.Entities;
using Induva.Domain.Enums;

namespace Induva.Application.Services;

public interface IConnectionService
{
    Task<Result<int>> SendAsync(SendConnectionRequestDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequestDto>> GetReceivedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequestDto>> GetSentAsync(CancellationToken ct = default);
    Task<Result> RespondAsync(int requestId, bool accept, CancellationToken ct = default);
}

public sealed class ConnectionService(IUnitOfWork uow, ICurrentUserService user) : IConnectionService
{
    public async Task<Result<int>> SendAsync(SendConnectionRequestDto dto, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result<int>.Fail(ErrorKind.Forbidden, "Please log in.");

        if (string.Equals(user.UserId, dto.ToUserId, StringComparison.OrdinalIgnoreCase))
            return Result<int>.Fail(ErrorKind.Validation, "You cannot send a connection request to yourself.");

        var existingPending = await uow.Repository<ConnectionRequest>().ListAsync(
            r => r.FromUserId == user.UserId
                 && r.ToUserId == dto.ToUserId
                 && r.RelatedType == dto.RelatedType
                 && r.RelatedId == dto.RelatedId
                 && r.Status == ConnectionStatus.Pending,
            ct);

        if (existingPending.Count > 0)
            return Result<int>.Fail(ErrorKind.Validation, "A pending connection request already exists for this opportunity.");

        var entity = new ConnectionRequest
        {
            FromUserId = user.UserId,
            ToUserId = dto.ToUserId,
            RelatedType = dto.RelatedType,
            RelatedId = dto.RelatedId,
            Message = string.IsNullOrWhiteSpace(dto.Message) ? "I would like to connect regarding this opportunity." : dto.Message.Trim(),
            Status = ConnectionStatus.Pending
        };

        await uow.Repository<ConnectionRequest>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<IReadOnlyList<ConnectionRequestDto>> GetReceivedAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) return [];

        var list = await uow.Repository<ConnectionRequest>().ListAsync(r => r.ToUserId == user.UserId, ct);
        return list.OrderByDescending(r => r.CreatedAtUtc).Select(r => new ConnectionRequestDto(
            r.Id,
            r.FromUserId,
            "Requester",
            r.ToUserId,
            "You",
            r.RelatedType,
            r.RelatedId,
            $"{r.RelatedType} #{r.RelatedId}",
            r.Message,
            r.Status,
            r.CreatedAtUtc,
            r.RespondedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<ConnectionRequestDto>> GetSentAsync(CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) return [];

        var list = await uow.Repository<ConnectionRequest>().ListAsync(r => r.FromUserId == user.UserId, ct);
        return list.OrderByDescending(r => r.CreatedAtUtc).Select(r => new ConnectionRequestDto(
            r.Id,
            r.FromUserId,
            "You",
            r.ToUserId,
            "Recipient",
            r.RelatedType,
            r.RelatedId,
            $"{r.RelatedType} #{r.RelatedId}",
            r.Message,
            r.Status,
            r.CreatedAtUtc,
            r.RespondedAtUtc)).ToList();
    }

    public async Task<Result> RespondAsync(int requestId, bool accept, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId))
            return Result.Fail(ErrorKind.Forbidden, "Please log in.");

        var req = await uow.Repository<ConnectionRequest>().GetByIdAsync(requestId, ct);
        if (req is null) return Result.Fail(ErrorKind.NotFound, "Connection request not found.");

        if (req.ToUserId != user.UserId)
            return Result.Fail(ErrorKind.Forbidden, "You can only respond to requests sent to you.");

        if (req.Status != ConnectionStatus.Pending)
            return Result.Fail(ErrorKind.Validation, "This request has already been answered.");

        req.Status = accept ? ConnectionStatus.Accepted : ConnectionStatus.Declined;
        req.RespondedAtUtc = DateTime.UtcNow;

        uow.Repository<ConnectionRequest>().Update(req);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

