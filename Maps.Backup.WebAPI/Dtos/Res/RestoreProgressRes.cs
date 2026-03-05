namespace Maps.Backup.WebAPI.Dtos.Res
{
    public class RestoreProgressRes
    {
        /// <summary>当前步骤</summary>
        public string Step { get; set; } = string.Empty;

        /// <summary>恢复进度（0-100）</summary>
        public int Progress { get; set; }

        /// <summary>进度描述信息</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>目标数据库名</summary>
        public string DBName { get; set; } = string.Empty;

        /// <summary>是否完成（最后一条数据为true）</summary>
        public bool IsSuccess { get; set; }
    }
}
