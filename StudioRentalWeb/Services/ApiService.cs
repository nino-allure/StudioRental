using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace StudioRentalWeb.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ErrorHandlingService _errorHandling;
        private readonly string _baseUrl;

        public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ErrorHandlingService errorHandling, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _errorHandling = errorHandling;
            _baseUrl = configuration.GetValue<string>("ApiSettings:BaseUrl") ?? "https://localhost:7175/api";
        }

        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<(T? Data, ErrorHandlingService.ApiErrorResponse? Error)> GetAsync<T>(string endpoint)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<T>(content);
                        return (data, null);
                    }
                    catch (JsonException jex)
                    {
                        return (default, new ErrorHandlingService.ApiErrorResponse { StatusCode = (int)response.StatusCode, Title = "Ошибка формата данных", Message = "Ошибка при обработке ответа от сервера" });
                    }
                }
                return (default, await _errorHandling.HandleErrorResponse(response));
            }
            catch (Exception ex)
            {
                return (default, new ErrorHandlingService.ApiErrorResponse { StatusCode = 0, Title = "Ошибка соединения", Message = ex.Message });
            }
        }

        public async Task<(T? Data, ErrorHandlingService.ApiErrorResponse? Error)> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                AddAuthorizationHeader();
                var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (string.IsNullOrEmpty(responseContent))
                        return (default, null);

                    return (JsonConvert.DeserializeObject<T>(responseContent), null);
                }

                return (default, await _errorHandling.HandleErrorResponse(response));
            }
            catch (Exception ex)
            {
                return (default, new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка соединения",
                    Message = ex.Message
                });
            }
        }

        public async Task<(bool Success, ErrorHandlingService.ApiErrorResponse? Error)> PutAsync(string endpoint, object data)
        {
            try
            {
                AddAuthorizationHeader();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}/{endpoint}", content);

                if (response.IsSuccessStatusCode) return (true, null);
                return (false, await _errorHandling.HandleErrorResponse(response));
            }
            catch (Exception ex)
            {
                return (false, new ErrorHandlingService.ApiErrorResponse { StatusCode = 0, Title = "Ошибка", Message = ex.Message });
            }
        }

        public async Task<(bool Success, ErrorHandlingService.ApiErrorResponse? Error)> DeleteAsync(string endpoint)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode) return (true, null);
                return (false, await _errorHandling.HandleErrorResponse(response));
            }
            catch (Exception ex)
            {
                return (false, new ErrorHandlingService.ApiErrorResponse { StatusCode = 0, Title = "Ошибка", Message = ex.Message });
            }
        }

        public async Task<(byte[]? Data, ErrorHandlingService.ApiErrorResponse? Error)> DownloadFileAsync(string endpoint)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    return (await response.Content.ReadAsByteArrayAsync(), null);
                }
                return (null, await _errorHandling.HandleErrorResponse(response));
            }
            catch (Exception ex)
            {
                return (null, new ErrorHandlingService.ApiErrorResponse { StatusCode = 0, Title = "Ошибка", Message = ex.Message });
            }
        }

        public async Task<(T? Data, ErrorHandlingService.ApiErrorResponse? Error)> PostWithFileAsync<T>(string endpoint, object data, IFormFile? file = null)
        {
            try
            {
                AddAuthorizationHeader();
                using var formData = new MultipartFormDataContent();

                var properties = data.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(data);
                    if (value != null)
                    {
                        // Используем имя свойства как есть (PascalCase для совместимости с DTO на бэкенде)
                        formData.Add(new StringContent(value.ToString() ?? ""), prop.Name);
                    }
                }

                if (file != null && file.Length > 0)
                {
                    using var stream = new MemoryStream();
                    await file.CopyToAsync(stream);
                    var fileContent = new ByteArrayContent(stream.ToArray());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    formData.Add(fileContent, "Image", file.FileName);
                }

                var response = await _httpClient.PostAsync($"{_baseUrl}/{endpoint}", formData);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (JsonConvert.DeserializeObject<T>(responseContent), null);
                }
                return (default, await _errorHandling.HandleErrorResponse(response));
            }
            catch (Exception ex)
            {
                return (default, new ErrorHandlingService.ApiErrorResponse { StatusCode = 0, Title = "Ошибка", Message = ex.Message });
            }
        }

        public async Task<(bool Success, ErrorHandlingService.ApiErrorResponse? Error)> PutWithFileAsync(string endpoint, object data, IFormFile? file = null)
        {
            try
            {
                AddAuthorizationHeader();
                using var formData = new MultipartFormDataContent();

                var properties = data.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(data);
                    if (value != null)
                    {
                        formData.Add(new StringContent(value.ToString() ?? ""), prop.Name);
                    }
                }

                if (file != null && file.Length > 0)
                {
                    using var stream = new MemoryStream();
                    await file.CopyToAsync(stream);
                    var fileContent = new ByteArrayContent(stream.ToArray());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    formData.Add(fileContent, "Image", file.FileName);
                }

                var response = await _httpClient.PutAsync($"{_baseUrl}/{endpoint}", formData);

                if (response.IsSuccessStatusCode) return (true, null);
                return (false, await _errorHandling.HandleErrorResponse(response));
            }
            catch (Exception ex)
            {
                return (false, new ErrorHandlingService.ApiErrorResponse { StatusCode = 0, Title = "Ошибка", Message = ex.Message });
            }
        }
    }
}