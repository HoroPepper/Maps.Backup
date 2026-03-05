using Maps.Backup.WebAPI.Dtos.Dto;

namespace Maps.Backup.WebAPI.Dtos.Req
{
    public class UserQueryReq
    {
        public string QueryMode { get; set; }

        public UserDto SingleQueryParam { get; set; }

        public List<UserDto> AndQueryParam { get; set; }

        public List<UserDto> OrQueryParam { get; set; }
    }
}
