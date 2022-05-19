namespace ListServiceManagement.Extensions
{
    public class QueryOptions
    {
        public QueryOptions()
        {
            CurrentPage = 1;
            PageSize = 1;
            SortField = "ApplicationRegistration_Id";
            SortOrder = SortOrder.ASC;
        }

        public string SearchString { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string SortField { get; set; }
        public SortOrder SortOrder { get; set; }

        public string Sort
        {
            get
            {
                return string.Format("{0} {1}",
                SortField, SortOrder.ToString());
            }
        }
    }

    public enum SortOrder
    {
        ASC,
        DESC
    }
}