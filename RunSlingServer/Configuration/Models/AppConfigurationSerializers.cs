﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace RunSlingServer.Configuration.Models;


/// <summary>
/// Collection of classes that implement the serialization of the appsettings.json file
/// These classes will be generated by .NET at compile time.
/// </summary>


[JsonSerializable(typeof(AppConfiguration))]
[JsonSerializable(typeof(AppSettings))]

[JsonSerializable(typeof(LoggingSettings))]
[JsonSerializable(typeof(LogLevelSettings))]
[JsonSerializable(typeof(UrlSettings))]

[JsonSerializable(typeof(EndpointsSettings))]
[JsonSerializable(typeof(EndpointsSettingsConverter))]

[JsonSerializable(typeof(KestrelSettings))]
public partial class JsonContext : JsonSerializerContext
{
}

/// <summary>
/// Hand made JSON converter for EndpointsSettings that does not write HTTPS if it is null, but can read it if is not null.
/// </summary>
public class EndpointsSettingsConverter : JsonConverter<EndpointsSettings>
{
    public override EndpointsSettings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var endpointsSettings = new EndpointsSettings();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return endpointsSettings;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new JsonException("Could not read property name");
                }

                reader.Read(); // advance to the value
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                switch (propertyName)
                {
                    case "Http":
                        endpointsSettings.Http = JsonSerializer.Deserialize<UrlSettings>(ref reader, options);
                        break;

                    case "Https":
                        endpointsSettings.Https = JsonSerializer.Deserialize<UrlSettings>(ref reader, options);
                        break;

                    default:
                        throw new JsonException($"Expected 'Http' or 'Https' but was {propertyName}");
                }
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            }
        }
        throw new JsonException();
    }



    // This code manually writes the properties of the Http and Https objects, checking for null values before writing.
    // This should achieve the same result as using the JsonIgnore attribute with JsonIgnoreCondition.WhenWritingNull.
    public override void Write(Utf8JsonWriter writer, EndpointsSettings value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Http != null && !string.IsNullOrEmpty(value.Http.Url))
        {
            writer.WritePropertyName("Http");
            writer.WriteStartObject();
            writer.WriteString("Url", value.Http.Url);
            writer.WriteEndObject();
        }
        if (value.Https != null && !string.IsNullOrEmpty(value.Https.Url))
        {
            writer.WritePropertyName("Https");
            writer.WriteStartObject();
            writer.WriteString("Url", value.Https.Url);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }
}


[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppConfiguration))]

public partial class ApplicationSettingsSerializerContext : JsonSerializerContext
{

}

