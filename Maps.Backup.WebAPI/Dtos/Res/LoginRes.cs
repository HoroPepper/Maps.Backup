namespace Maps.Backup.WebAPI.Dtos.Res
{
    public class LoginRes
    {
        public bool IsSucceed { get; set; }

        public string Message { get; set; }

        public string Token { get; set; }

        public string RefreshToken { get; set; }

        public DateTime TokenExpireTime { get; set; }
    }
}
