namespace WebApplication1.Helpers
{
    /// <summary>
    /// Generic pagination helper for lists
    /// </summary>
    public class PaginatedList<T>
    {
        public List<T> Items { get; set; }
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            TotalCount = count;
            PageSize = pageSize;
            Items = items;
        }

        public bool HasPreviousPage => PageIndex > 1;

        public bool HasNextPage => PageIndex < TotalPages;

        public int StartItemNumber => (PageIndex - 1) * PageSize + 1;

        public int EndItemNumber => Math.Min(PageIndex * PageSize, TotalCount);

        /// <summary>
        /// Creates a paginated list from an IQueryable
        /// </summary>
        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source,
            int pageIndex,
            int pageSize)
        {
            var count = await Task.Run(() => source.Count());
            var items = await Task.Run(() =>
                source.Skip((pageIndex - 1) * pageSize)
                      .Take(pageSize)
                      .ToList()
            );

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets page numbers to display in pagination controls
        /// </summary>
        public int[] GetPageNumbers(int maxPagesToShow = 5)
        {
            if (TotalPages <= maxPagesToShow)
            {
                return Enumerable.Range(1, TotalPages).ToArray();
            }

            var halfMaxPages = maxPagesToShow / 2;
            var startPage = Math.Max(1, PageIndex - halfMaxPages);
            var endPage = Math.Min(TotalPages, startPage + maxPagesToShow - 1);

            // Adjust start page if we're near the end
            if (endPage - startPage + 1 < maxPagesToShow)
            {
                startPage = Math.Max(1, endPage - maxPagesToShow + 1);
            }

            return Enumerable.Range(startPage, endPage - startPage + 1).ToArray();
        }
    }

    /// <summary>
    /// Extension methods for pagination
    /// </summary>
    public static class PaginationExtensions
    {
        /// <summary>
        /// Converts IQueryable to PaginatedList
        /// </summary>
        public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> source,
            int pageNumber,
            int pageSize)
        {
            return await PaginatedList<T>.CreateAsync(source, pageNumber, pageSize);
        }
    }
}