using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Common.Abstractions.Results;
using Microsoft.Extensions.Options;
using Users.Application.Abstractions;
using Users.Application.Common.Models;
using Users.Domain.Errors;
using Users.Infrastructure.Configuration;
using Users.Infrastructure.Errors;

namespace Users.Infrastructure.Services;

  public class KeycloakService(HttpClient httpClient, IOptions<KeycloakSettings> settings) : IKeycloakService
    {
        private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        private readonly KeycloakSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

    public async Task<Result<KeycloakUserResponse>> GetUserByEmailAsync(string email)
        {
            var tokenResult = await EnsureAccessTokenAsync();
            if (tokenResult.IsFailure)
            {
                return Result<KeycloakUserResponse>.Failure(tokenResult.Error);
            }

            var requestUrl = $"/admin/realms/{_settings.Realm}/users?email={Uri.EscapeDataString(email)}&exact=true";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Result<KeycloakUserResponse>.Failure(KeycloakErrors.SearchFailed(errorContent));
            }

            var users = await response.Content.ReadFromJsonAsync<List<KeycloakUser>>();

            if (users == null || users.Count == 0)
            {
                return Result<KeycloakUserResponse>.Failure(UserErrors.NotFoundByEmail(email));
            }

            var user = users[0];
            return Result<KeycloakUserResponse>.Success(
                new KeycloakUserResponse(user.Id, user.Email ?? "", user.Username ?? "", user.FirstName ?? "", user.LastName ?? ""));
        }

        public async Task<Result<KeycloakUserResponse>> CreateUserAsync(UserForCreationDto user)
        {
            var tokenResult = await EnsureAccessTokenAsync();
            if (tokenResult.IsFailure)
            {
                return Result<KeycloakUserResponse>.Failure(tokenResult.Error);
            }

            var nameParts = user.Name.Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : user.Name;
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            var keycloakUser = new KeycloakUserCreate
            {
                Username = user.Username,
                Email = user.Email,
                FirstName = firstName,
                LastName = lastName,
                Enabled = true,
                EmailVerified = true,
                Credentials = new List<KeycloakCredential>
                {
                    new()
                    {
                        Type = "password",
                        Value = user.Password,
                        Temporary = false
                    }
                }
            };

            var requestUrl = $"/admin/realms/{_settings.Realm}/users";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = JsonContent.Create(keycloakUser);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Result<KeycloakUserResponse>.Failure(KeycloakErrors.CreateFailed(errorContent));
            }

            // Keycloak returns 201 with Location header containing the user ID
            var locationHeader = response.Headers.Location?.ToString();
            var userId = locationHeader?.Split('/').LastOrDefault() ?? "";

            return Result<KeycloakUserResponse>.Success(
                new KeycloakUserResponse(userId, user.Email, user.Username, firstName, lastName));
        }

        private async Task<Result<bool>> EnsureAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return Result<bool>.Success(true);
            }

            var tokenUrl = $"/realms/{_settings.Realm}/protocol/openid-connect/token";

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret
            });

            var response = await _httpClient.PostAsync(tokenUrl, tokenRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Result<bool>.Failure(KeycloakErrors.TokenRequestFailed(errorContent));
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (tokenResponse == null)
            {
                return Result<bool>.Failure(KeycloakErrors.TokenParseFailed());
            }

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30); // Refresh 30 seconds early

            return Result<bool>.Success(true);
        }

        private class KeycloakUser
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("email")]
            public string? Email { get; set; }

            [JsonPropertyName("username")]
            public string? Username { get; set; }

            [JsonPropertyName("firstName")]
            public string? FirstName { get; set; }

            [JsonPropertyName("lastName")]
            public string? LastName { get; set; }
        }

        private class KeycloakUserCreate
        {
            [JsonPropertyName("username")]
            public string Username { get; set; } = "";

            [JsonPropertyName("email")]
            public string Email { get; set; } = "";

            [JsonPropertyName("firstName")]
            public string FirstName { get; set; } = "";

            [JsonPropertyName("lastName")]
            public string LastName { get; set; } = "";

            [JsonPropertyName("enabled")]
            public bool Enabled { get; set; }

            [JsonPropertyName("emailVerified")]
            public bool EmailVerified { get; set; }

            [JsonPropertyName("credentials")]
            public List<KeycloakCredential> Credentials { get; set; } = new();
        }

        private class KeycloakCredential
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "";

            [JsonPropertyName("value")]
            public string Value { get; set; } = "";

            [JsonPropertyName("temporary")]
            public bool Temporary { get; set; }
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = "";

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }