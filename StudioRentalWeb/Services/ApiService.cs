using Newtonsoft.Json;
using System.Text;
using StudioRentalWeb.Services;

namespace StudioRentalWeb.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ErrorHandlingService _errorHandling;
        private const string BaseUrl = "https://localhost:7175/api";

        public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ErrorHandlingService errorHandling)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _errorHandling = errorHandling;
        }

        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        public async Task<(T? Data, ErrorHandlingService.ApiErrorResponse? Error)> GetAsync<T>(string endpoint)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<T>(content);
                    return (data, null);
                }
                else
                {
                    var error = await _errorHandling.HandleErrorResponse(response);
                    return (default, error);
                }
            }
            catch (HttpRequestException ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка соединения",
                    Message = $"Не удалось подключиться к серверу: {ex.Message}"
                };
                return (default, error);
            }
            catch (Exception ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка",
                    Message = ex.Message
                };
                return (default, error);
            }
        }

        public async Task<(T? Data, ErrorHandlingService.ApiErrorResponse? Error)> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                AddAuthorizationHeader();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<T>(responseContent);
                    return (result, null);
                }
                else
                {
                    var error = await _errorHandling.HandleErrorResponse(response);
                    return (default, error);
                }
            }
            catch (HttpRequestException ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка соединения",
                    Message = $"Не удалось подключиться к серверу: {ex.Message}"
                };
                return (default, error);
            }
            catch (Exception ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка",
                    Message = ex.Message
                };
                return (default, error);
            }
        }

        public async Task<(bool Success, ErrorHandlingService.ApiErrorResponse? Error)> PutAsync(string endpoint, object data)
        {
            try
            {
                AddAuthorizationHeader();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BaseUrl}/{endpoint}", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                else
                {
                    var error = await _errorHandling.HandleErrorResponse(response);
                    return (false, error);
                }
            }
            catch (HttpRequestException ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка соединения",
                    Message = $"Не удалось подключиться к серверу: {ex.Message}"
                };
                return (false, error);
            }
            catch (Exception ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка",
                    Message = ex.Message
                };
                return (false, error);
            }
        }

        public async Task<(bool Success, ErrorHandlingService.ApiErrorResponse? Error)> DeleteAsync(string endpoint)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.DeleteAsync($"{BaseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                else
                {
                    var error = await _errorHandling.HandleErrorResponse(response);
                    return (false, error);
                }
            }
            catch (HttpRequestException ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка соединения",
                    Message = $"Не удалось подключиться к серверу: {ex.Message}"
                };
                return (false, error);
            }
            catch (Exception ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка",
                    Message = ex.Message
                };
                return (false, error);
            }
        }
    }
}