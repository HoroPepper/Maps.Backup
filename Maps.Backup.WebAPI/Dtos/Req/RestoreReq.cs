namespace Maps.Backup.WebAPI.Dtos.Req
{
    public class RestoreReq
    {
        public string BackupFilePath { get; set; }

        public string QANO { get; set; }

        public string TicketNO { get; set; }

        public string VersionNO { get; set; }
    }
}
