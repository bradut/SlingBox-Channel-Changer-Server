using System.Text.Json;
using Domain.Models;

namespace Domain.Helpers
{
    /// <summary>
    /// Handmade JSON (de)serialization to avoid warning messages during AOT compilation
    /// due to reflexion when using reflexion-based serializers
    /// </summary>
    public static class SlingBoxServerSerializer
    {
        private static readonly string SlingBoxes = nameof(SlingBoxServerStatus.SlingBoxes).ToJsonName();
        private static readonly string SlingBoxId = nameof(SlingBoxStatus.SlingBoxId).ToJsonName();

        private static readonly string CurrentChannelNumber = nameof(SlingBoxStatus.CurrentChannelNumber).ToJsonName();
        private static readonly string LastChannelNumber = nameof(SlingBoxStatus.LastChannelNumber).ToJsonName();
        private static readonly string IsAnalogue = nameof(SlingBoxStatus.IsAnalogue).ToJsonName();

        private static readonly string UrlBase = nameof(SlingBoxServerStatus.UrlBase).ToJsonName();
        private static readonly string TvGuideUrl = nameof(SlingBoxServerStatus.TvGuideUrl).ToJsonName();
        private static readonly string SlingRemoteControlServiceUrl = nameof(SlingBoxServerStatus.SlingRemoteControlServiceUrl).ToJsonName();

        private static readonly string LastHeartBeatTimeStamp = nameof(SlingBoxStatus.LastHeartBeatTimeStamp).ToJsonName();
        private static readonly string TvGuideUrlForSlingBox = nameof(SlingBoxStatus.TvGuideUrl).ToJsonName();


        public static string SerializeToJson(SlingBoxServerStatus serverStatus)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                writer.WritePropertyName(SlingBoxes);
                writer.WriteStartObject();

                foreach (var (slingBoxName, slingBoxStatus) in serverStatus.SlingBoxes)
                {
                    writer.WritePropertyName(slingBoxName);
                    writer.WriteStartObject();

                    writer.WriteString(SlingBoxId, slingBoxStatus.SlingBoxId);
                    writer.WriteNumber(CurrentChannelNumber, slingBoxStatus.CurrentChannelNumber);
                    writer.WriteNumber(LastChannelNumber, slingBoxStatus.LastChannelNumber);
                    writer.WriteBoolean(IsAnalogue, slingBoxStatus.IsAnalogue);

                    if (slingBoxStatus.LastHeartBeatTimeStamp.HasValue)
                    {
                        writer.WriteString(LastHeartBeatTimeStamp, slingBoxStatus.LastHeartBeatTimeStamp.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }
                    else
                    {
                        writer.WriteNull(LastHeartBeatTimeStamp);
                    }

                    if (!string.IsNullOrWhiteSpace(slingBoxStatus.TvGuideUrl) && IsValidUri((slingBoxStatus.TvGuideUrl)))
                    {
                        writer.WriteString(TvGuideUrlForSlingBox, slingBoxStatus.TvGuideUrl);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();

                writer.WriteString(UrlBase, serverStatus.UrlBase);
                writer.WriteString(TvGuideUrl, serverStatus.TvGuideUrl);
                writer.WriteString(SlingRemoteControlServiceUrl, serverStatus.SlingRemoteControlServiceUrl);

                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }


        private static readonly string IsStreaming = nameof(SlingBoxStatus.IsStreaming).ToJsonName();

       
        
        // Data to be sent to the TVGuide WebSite
        public static string SerializeSlingBoxesStatusToJsonForTvGuideWebSite(List<SlingBoxStatus> slingBoxesStatus)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                writer.WritePropertyName(SlingBoxes);
                writer.WriteStartObject();

                foreach (var slingBoxStatus in slingBoxesStatus)
                {
                    writer.WritePropertyName(slingBoxStatus.SlingBoxName);
                    writer.WriteStartObject();

                    writer.WriteNumber(CurrentChannelNumber, slingBoxStatus.CurrentChannelNumber);
                    writer.WriteBoolean(IsStreaming, slingBoxStatus.IsStreaming);
                    
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();

                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        public static bool IsValidUri(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? result)
                   && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }



        private static string ToJsonName(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;
            return str.Length == 1 ? str.ToLower() : $"{char.ToLower(str[0]) + str.Substring(1)}";
        }
    }



}
