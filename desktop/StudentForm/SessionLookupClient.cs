using System.Net.Http.Json;
using System.Text.Json;

namespace StudentForm;

internal sealed class SessionLookupClient
{
    private readonly HttpClient _httpClient = new();

    public async Task<JoinSessionLookupDto?> LookupAsync(string backendBaseUrl, string sessionCode, string sessionToken, CancellationToken cancellationToken = default)
    {
        string baseUrl = backendBaseUrl.TrimEnd('/');
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"{baseUrl}/api/session-access/lookup",
            new
            {
                sessionCode,
                sessionToken,
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string message = await response.Content.ReadAsStringAsync(cancellationToken);
            message = ExtractErrorMessage(message);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? $"Lookup failed with status {(int)response.StatusCode}."
                : message);
        }

        return await response.Content.ReadFromJsonAsync<JoinSessionLookupDto>(cancellationToken: cancellationToken);
    }

    private static string ExtractErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "";
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("message", out JsonElement messageElement))
            {
                return messageElement.GetString() ?? responseBody;
            }
        }
        catch (JsonException)
        {
            return responseBody;
        }

        return responseBody;
    }
}
