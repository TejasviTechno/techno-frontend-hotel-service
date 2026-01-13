using System.Text.Json;
using System.Text.Json.Serialization;

namespace XConnect.Models
{
    public class StringToIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //// If the value is a string, try to parse it to an integer
            //if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out int result))
            //{
            //    return result;
            //}
            //// Handle cases where it's already an int or if you need a fallback
            //if (reader.TokenType == JsonTokenType.Number)
            //{
            //    return reader.GetInt32();
            //}
            //// You could throw an exception or handle other cases as needed
            //throw new JsonException($"Unable to convert {reader.GetString()} to int.");


            if (reader.TokenType == JsonTokenType.String)
            {
                string stringValue = reader.GetString();

                if (string.IsNullOrWhiteSpace(stringValue)) // Treat empty string as 0
                {
                    return 0;
                }

                if (int.TryParse(stringValue, out int result))
                {
                    return result;
                }

                throw new JsonException($"Invalid value for CountryID: {stringValue}");
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }

            return 0; // Default fallback
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            // Standard int serialization
            writer.WriteNumberValue(value);
        }
    }
}
