using System.Text.Json;

namespace MessyOrderManagement.Tests;

/// <summary>
/// Extension methods for HttpClient that automatically use configured JSON options.
/// These options match the configuration in Program.cs (camelCase and IncludeFields).
/// 
/// Note: These extension methods will be used instead of the standard ones when both are in scope,
/// as they are defined in the same namespace as the test classes.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// JSON serializer options that match the application configuration.
    /// These options are used automatically by the extension methods below.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Reads the HTTP content and deserializes it as JSON using the configured options.
    /// This overload automatically uses the configured JSON options (camelCase, IncludeFields).
    /// </summary>
    public static Task<T?> ReadFromJsonAsync<T>(
        this HttpContent content,
        CancellationToken cancellationToken = default)
    {
        // Explicitly call the overload that takes JsonSerializerOptions
        return System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(content, DefaultJsonOptions, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with JSON content using the configured options.
    /// This overload automatically uses the configured JSON options (camelCase, IncludeFields).
    /// </summary>
    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        CancellationToken cancellationToken = default)
    {
        // Explicitly call the overload that takes JsonSerializerOptions
        return System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, requestUri, value, DefaultJsonOptions, cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with JSON content using the configured options.
    /// This overload automatically uses the configured JSON options (camelCase, IncludeFields).
    /// </summary>
    public static Task<HttpResponseMessage> PutAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        CancellationToken cancellationToken = default)
    {
        // Explicitly call the overload that takes JsonSerializerOptions
        return System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync(client, requestUri, value, DefaultJsonOptions, cancellationToken);
    }
}