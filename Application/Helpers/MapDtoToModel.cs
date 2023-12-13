using Application.Models.DTOs;
using Domain.Models;

namespace Application.Helpers
{
    public class MapSlingBoxServerStatusDtoToModel
    {
        public static SlingBoxServerStatus? Map(SlingBoxServerStatusDto? serverStatusDto)
        {
            if (serverStatusDto is null)
                return null;

            var slingBoxStatusCollection = new List<SlingBoxStatus>();

            foreach (var slingBoxStatusDto in serverStatusDto.SlingBoxes)
            {
                slingBoxStatusDto.Value.SlingBoxName = slingBoxStatusDto.Key;

                var slingBoxModel = MapSlingBoxStatusDtoToModel.Map(slingBoxStatusDto.Value);

                if (slingBoxModel is null)
                    continue;

                slingBoxStatusCollection.Add(slingBoxModel);
            }

            var serverStatus = new SlingBoxServerStatus(slingBoxStatusCollection)
            {
                UrlBase = serverStatusDto.UrlBase,
                TvGuideUrl = serverStatusDto.TvGuideUrl,
                SlingRemoteControlServiceUrl = serverStatusDto.SlingRemoteControlServiceUrl
            };
            
            return serverStatus;
        }
    }


    public class MapSlingBoxStatusDtoToModel
    {
        public static SlingBoxStatus? Map(SlingBoxStatusDto? slingBoxStatusDto)
        {
            if (slingBoxStatusDto is null)
                return null;

            var slingBoxStatus = new SlingBoxStatus
            (
                 slingBoxName: slingBoxStatusDto.SlingBoxName,
                 slingBoxId: slingBoxStatusDto.SlingBoxId,
                 currentChannelNumber: slingBoxStatusDto.CurrentChannelNumber,
                 lastChannelNumber: slingBoxStatusDto.LastChannelNumber,
                 isAnalogue: slingBoxStatusDto.IsAnalogue,
                 lastHeartBeatTimeStamp: slingBoxStatusDto.LastHeartBeatTimeStamp,
                 tvGuideUrl: slingBoxStatusDto.TvGuideUrl
            );
            
            return slingBoxStatus;
        }
    }
}
