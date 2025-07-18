namespace AlarmService.Extensions;

using System.Text.Json;
using System.Text.Json.Nodes; 
using System;

public static class JsonConverterExtensions
{
    /// <summary>
    /// Versucht, ein JsonElement in ein JsonArray zu konvertieren.
    /// </summary>
    /// <param name="jsonElement">Das zu konvertierende JsonElement.</param>
    /// <returns>Ein JsonArray, wenn die Konvertierung erfolgreich ist und das JsonElement ein Array repräsentiert; andernfalls null.</returns>
    public static JsonArray? ToJsonArray(this JsonElement jsonElement)
    {
        if (jsonElement.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("Fehler: Das JsonElement ist kein Array und kann nicht in ein JsonArray konvertiert werden.");
            return null;
        }

        try
        {
            return jsonElement.Deserialize<JsonArray>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Fehler beim Deserialisieren des JsonElement in ein JsonArray: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ein unerwarteter Fehler ist beim Konvertieren aufgetreten: {ex.Message}");
            return null;
        }
    }
}