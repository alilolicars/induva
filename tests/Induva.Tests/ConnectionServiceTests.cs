using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class ConnectionServiceTests
{
    private static (ConnectionService svc, ApplicationDbContext db, ICurrentUserService user) CreateContext(string? userId)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(!string.IsNullOrEmpty(userId));

        return (new ConnectionService(new UnitOfWork(db), user), db, user);
    }

    [Fact]
    public async Task Cannot_send_connection_request_to_self()
    {
        var (svc, _, _) = CreateContext("user-1");

        var r = await svc.SendAsync(new SendConnectionRequestDto("user-1", MatchEntityType.Factory, 10, "Hello self"));

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Validation, r.Kind);
        Assert.Contains("yourself", r.Error);
    }

    [Fact]
    public async Task Cannot_duplicate_pending_connection_request()
    {
        var (svc, _, _) = CreateContext("sender-1");

        var r1 = await svc.SendAsync(new SendConnectionRequestDto("recipient-2", MatchEntityType.Factory, 10, "First note"));
        Assert.True(r1.Succeeded);

        var r2 = await svc.SendAsync(new SendConnectionRequestDto("recipient-2", MatchEntityType.Factory, 10, "Second duplicate note"));
        Assert.False(r2.Succeeded);
        Assert.Equal(ErrorKind.Validation, r2.Kind);
        Assert.Contains("already exists", r2.Error);
    }

    [Fact]
    public async Task Only_recipient_can_respond_to_request()
    {
        var (svcSender, db, _) = CreateContext("sender-1");
        var rCreate = await svcSender.SendAsync(new SendConnectionRequestDto("recipient-2", MatchEntityType.Startup, 5, "Hello"));
        Assert.True(rCreate.Succeeded);
        var reqId = rCreate.Value;

        // An unrelated user cannot respond
        var userIntruder = Substitute.For<ICurrentUserService>();
        userIntruder.UserId.Returns("intruder");
        userIntruder.IsAuthenticated.Returns(true);
        var svcIntruder = new ConnectionService(new UnitOfWork(db), userIntruder);

        var rIntruder = await svcIntruder.RespondAsync(reqId, accept: true);
        Assert.False(rIntruder.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rIntruder.Kind);

        // Recipient can respond
        var userRecipient = Substitute.For<ICurrentUserService>();
        userRecipient.UserId.Returns("recipient-2");
        userRecipient.IsAuthenticated.Returns(true);
        var svcRecipient = new ConnectionService(new UnitOfWork(db), userRecipient);

        var rAccept = await svcRecipient.RespondAsync(reqId, accept: true);
        Assert.True(rAccept.Succeeded);

        var req = await db.ConnectionRequests.FindAsync(reqId);
        Assert.Equal(ConnectionStatus.Accepted, req!.Status);
        Assert.NotNull(req.RespondedAtUtc);
    }
}

