namespace ClaudePlayground.Domain.Common;

/// <summary>
/// Represents a paginated result set with metadata
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether this is the first page
    /// </summary>
    public bool IsFirstPage => Page == 1;

    /// <summary>
    /// Whether this is the last page
    /// </summary>
    public bool IsLastPage => Page >= TotalPages;
}
