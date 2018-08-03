namespace PivotForWPF
{
    public enum ScrollingStyle
    {
        /// <summary>
        /// 舊有內容整頁滑動移出後，新的內容再整頁滑動移入
        /// </summary>
        Whole,

        /// <summary>
        /// 舊有內容整頁滑動移出，同時新的內容整頁滑動移入
        /// </summary>
        Connected,

        /// <summary>
        /// 所有內容都是單項，並在滑動時自動判斷該滑動到哪個元件
        /// </summary>
        Single,

        /// <summary>
        /// 系統預設滑動行為
        /// </summary>
        System,
    }
}