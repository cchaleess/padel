using PadelMatch.Domain.Matches;

namespace PadelMatch.Domain.Tests.Matches;

public class MatchAccessRequestTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ANewRequestIsPending()
    {
        var request = MatchAccessRequest.Create(Guid.NewGuid(), Guid.NewGuid(), requestedPosition: 2, NowUtc);

        Assert.Equal(AccessRequestStatus.Pending, request.Status);
        Assert.Equal(2, request.RequestedPosition);
        Assert.Null(request.ResolvedAtUtc);
    }

    [Fact]
    public void APendingRequestCanBeApprovedOrRejected()
    {
        var approved = MatchAccessRequest.Create(Guid.NewGuid(), Guid.NewGuid(), requestedPosition: 2, NowUtc);
        var rejected = MatchAccessRequest.Create(Guid.NewGuid(), Guid.NewGuid(), requestedPosition: 2, NowUtc);

        approved.Approve(NowUtc.AddMinutes(1));
        rejected.Reject(NowUtc.AddMinutes(1));

        Assert.Equal(AccessRequestStatus.Approved, approved.Status);
        Assert.Equal(AccessRequestStatus.Rejected, rejected.Status);
        Assert.Equal(NowUtc.AddMinutes(1), approved.ResolvedAtUtc);
    }

    [Fact]
    public void AResolvedRequestCannotChange()
    {
        var request = MatchAccessRequest.Create(Guid.NewGuid(), Guid.NewGuid(), requestedPosition: 2, NowUtc);
        request.Reject(NowUtc);

        Assert.Throws<InvalidOperationException>(() => request.Approve(NowUtc));
        Assert.Throws<InvalidOperationException>(() => request.Reject(NowUtc));
    }

    [Fact]
    public void ARequestedPositionOutsideZeroToThreeIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchAccessRequest.Create(Guid.NewGuid(), Guid.NewGuid(), requestedPosition: 4, NowUtc));
    }
}
