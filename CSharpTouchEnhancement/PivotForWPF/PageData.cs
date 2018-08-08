namespace PivotForWPF
{
    public struct PageData
    {
        /// <summary>
        /// 一個頁面可以顯示幾個 item
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 總共幾個頁面
        /// </summary>
        public int PageCount { get; set; }
    }
}