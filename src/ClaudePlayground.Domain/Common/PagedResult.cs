namespace ClaudePlayground.Domain.Common;

/// <summary>
/// Represents a paginated result set with metadata for efficient data retrieval and navigation.
/// Includes calculated properties for pagination UI controls.
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
/// <param name="Items">The collection of items for the current page</param>
/// <param name="TotalCount">The total number of items across all pages</param>
/// <param name="Page">The current page number (1-based)</param>
/// <param name="PageSize">The number of items per page</param>
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
