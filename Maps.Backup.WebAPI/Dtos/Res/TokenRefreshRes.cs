namespace Maps.Backup.WebAPI.Dtos.Res
{
    public class TokenRefreshRes
    {
        public bool IsSucceed { get; set; }

        public string Message { get; set; }

        public string Token { get; set; }

        public DateTime TokenExpireTime { get; set; }

    }
}
