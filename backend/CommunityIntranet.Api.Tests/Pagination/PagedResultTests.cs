using CommunityIntranet.BuildingBlocks.Pagination;

namespace CommunityIntranet.Api.Tests.Pagination;

public sealed class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    public void TotalPagesRoundsUp(long totalCount, int pageSize, long expected)
    {
        var result = new PagedResult<string>([], 1, pageSize, totalCount);

        Assert.Equal(expected, result.TotalPages);
    }
}
