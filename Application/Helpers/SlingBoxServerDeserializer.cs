using System.Globalization;
using System.Text.Json;
using Application.Models.DTOs;

namespace Application.Helpers
{

    /// <summary>
    /// Handmade JSON (de)serialization to avoid warning messages during AOT compilation
    /// due to reflexion when using reflexion-based serializers
    /// </summary>
    public static class SlingBoxServerDeserializer
    {

        private static readonly string SlingBoxes = nameof(SlingBoxServerStatusDto.SlingBoxes).ToJsonName();
        private static readonly string SlingBoxId = nameof(SlingBoxStatusDto.SlingBoxId).ToJsonName();

        private static readonly string CurrentChannelNumber = nameof(SlingBoxStatusDto.CurrentChannelNumber).ToJsonName();
        private static readonly string LastChannelNumber = nameof(SlingBoxStatusDto.LastChannelNumber).ToJsonName();
        private static readonly string IsAnalogue = nameof(SlingBoxStatusDto.IsAnalogue).ToJsonName();

        private static readonly string UrlBase = nameof(SlingBoxServerStatusDto.UrlBase).ToJsonName();
        private static readonly string TvGuideUrl = nameof(SlingBoxServerStatusDto.TvGuideUrl).ToJsonName();
        private static readonly string SlingRemoteControlServiceUrl = nameof(SlingBoxServerStatusDto.SlingRemoteControlServiceUrl).ToJsonName();

        private static readonly string LastHeartBeatTimeStamp = nameof(SlingBoxStatusDto.LastHeartBeatTimeStamp).ToJsonName();
        private static readonly string TvGuideUrlForSlingBox = nameof(SlingBoxStatusDto.TvGuideUrl).ToJsonName();


        public static SlingBoxServerStatusDto DeserializeJson(string jsonString)
        {
            var data = new SlingBoxServerStatusDto();
            var jsonDocument = JsonDocument.Parse(jsonString);

            var root = jsonDocument.RootElement;
            var slingBoxesElement = root.GetPropertyCaseInsensitive(SlingBoxes);
            var urlBaseElement = root.GetPropertyCaseInsensitive(UrlBase);

            data.UrlBase = urlBaseElement.GetString() ?? "sb-secret";
            data.TvGuideUrl = root.GetPropertyCaseInsensitive(TvGuideUrl).GetString() ?? "";
            data.SlingRemoteControlServiceUrl = root.GetPropertyCaseInsensitive(SlingRemoteControlServiceUrl).GetString() ?? "";

            data.SlingBoxes = new Dictionary<string, SlingBoxStatusDto>();

            foreach (var slingBoxProperty in slingBoxesElement.EnumerateObject())
            {
                var slingBoxName = slingBoxProperty.Name;
                var slingBoxElement = slingBoxProperty.Value;

                var slingBoxId = slingBoxElement.GetPropertyCaseInsensitive(SlingBoxId).GetString();
                var currentChannelNumber = slingBoxElement.GetPropertyCaseInsensitive(CurrentChannelNumber).GetInt32();
                var lastChannelNumber = slingBoxElement.GetPropertyCaseInsensitive(LastChannelNumber).GetInt32();
                var isAnalogue = slingBoxElement.GetPropertyCaseInsensitive(IsAnalogue).GetBoolean();
                var lastHeartBeatTimeStamp = slingBoxElement.GetPropertyCaseInsensitive(LastHeartBeatTimeStamp).ValueKind != JsonValueKind.Null
                        ? slingBoxElement.GetPropertyCaseInsensitive(LastHeartBeatTimeStamp).GetDateTimeFormatted()
                        : null;

                // Optional: the TVGuide URL for this SlingBox overrides server's TVGuide URL
                var tvGuideUrlForSlingBox = slingBoxElement.GetPropertyCaseInsensitive(TvGuideUrlForSlingBox).ValueKind == JsonValueKind.String
                    ? slingBoxElement.GetPropertyCaseInsensitive(TvGuideUrl).GetString()
                    : null;


                var slingBox = new SlingBoxStatusDto
                {
                    SlingBoxId = slingBoxId ?? "",
                    SlingBoxName = slingBoxName,
                    CurrentChannelNumber = currentChannelNumber,
                    LastChannelNumber = lastChannelNumber,
                    IsAnalogue = isAnalogue,
                    LastHeartBeatTimeStamp = lastHeartBeatTimeStamp,
                    TvGuideUrl = tvGuideUrlForSlingBox
                };

                data.SlingBoxes.Add(slingBoxName, slingBox);
            }

            return data;
        }


        private static DateTime? GetDateTimeFormatted(this JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null)
                return null;

            var strDateTime = element.GetString();
            if (string.IsNullOrWhiteSpace(strDateTime) ||
                strDateTime.Equals("null", StringComparison.CurrentCultureIgnoreCase)) // a bug in my serialization writes "null" instead of null
                return null;

            var dateTime = DateTime.ParseExact(strDateTime, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

            return dateTime;

        }

        private static string ToJsonName(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) 
                return string.Empty;

            return str.Length == 1 
                ? str.ToLower() 
                : $"{char.ToLower(str[0]) + str.Substring(1)}";
        }
    }


    // Extension method to get a property case insensitive, Use 'GetPropertyCaseInsensitive' instead of 'GetProperty'
    // https://stackoverflow.com/questions/70366457/how-to-force-system-text-json-jsonelement-getproperty-to-search-for-properties-u
    internal static class JsonElementExtensions
    {
        public static JsonElement GetPropertyCaseInsensitive(this JsonElement jsonElement, string propertyName)
        {
            foreach (var property in jsonElement.EnumerateObject().OfType<JsonProperty>())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    return property.Value;
            }

            return default;
        }
    }
}
