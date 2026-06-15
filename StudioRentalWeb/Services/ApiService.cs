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

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response ({endpoint}): Status={response.StatusCode}, Content={content?.Substring(0, Math.Min(200, content?.Length ?? 0))}...");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<T>(content);
                        return (data, null);
                    }
                    catch (JsonException jex)
                    {
                        Console.WriteLine($"JSON Deserialize Error: {jex.Message}");
                        var error = new ErrorHandlingService.ApiErrorResponse
                        {
                            StatusCode = (int)response.StatusCode,
                            Title = "Ошибка формата данных",
                            Message = "Ошибка при обработке ответа от сервера"
                        };
                        return (default, error);
                    }
                }
                else
                {
                    var error = await _errorHandling.HandleErrorResponse(response);
                    return (default, error);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
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
                Console.WriteLine($"Exception: {ex.Message}");
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
        public async Task<(string? FilePath, ErrorHandlingService.ApiErrorResponse? Error)> DownloadFileToDownloadsAsync(string endpoint, string fileName)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsByteArrayAsync();

                    string downloadsPath;

                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {

                        downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    }
                    else
                    {
                        downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    }

                    if (!Directory.Exists(downloadsPath))
                        Directory.CreateDirectory(downloadsPath);

                    var fullPath = Path.Combine(downloadsPath, fileName);
                    int counter = 1;
                    while (System.IO.File.Exists(fullPath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var extension = Path.GetExtension(fileName);
                        fullPath = Path.Combine(downloadsPath, $"{nameWithoutExt}_{counter}{extension}");
                        counter++;
                    }

                    await System.IO.File.WriteAllBytesAsync(fullPath, data);
                    return (fullPath, null);
                }
                else
                {
                    var error = await _errorHandling.HandleErrorResponse(response);
                    return (null, error);
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
                return (null, error);
            }
            catch (Exception ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка",
                    Message = ex.Message
                };
                return (null, error);
            }
        }

        /// <summary>
        /// Скачивание файла
        /// </summary>
        public async Task<(byte[]? Data, ErrorHandlingService.ApiErrorResponse? Error)> DownloadFileAsync(string endpoint)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsByteArrayAsync();
                    return (data, null);
                }
                else
                {
                    var error = await _errorHandling.HandleErrorResponse(response);
                    return (null, error);
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
                return (null, error);
            }
            catch (Exception ex)
            {
                var error = new ErrorHandlingService.ApiErrorResponse
                {
                    StatusCode = 0,
                    Title = "Ошибка",
                    Message = ex.Message
                };
                return (null, error);
            }
        }
        // StudioRentalWeb\Services\ApiService.cs - добавьте эти методы

        /// <summary>
        /// Создание студии с изображением
        /// </summary>
        public async Task<(T? Data, ErrorHandlingService.ApiErrorResponse? Error)> PostWithFileAsync<T>(string endpoint, object data, IFormFile? file = null)
        {
            try
            {
                AddAuthorizationHeader();

                using var formData = new MultipartFormDataContent();

                // Добавляем все свойства объекта в form-data
                var properties = data.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(data)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        formData.Add(new StringContent(value), prop.Name);
                    }
                }

                // Добавляем файл если есть
                if (file != null)
                {
                    using var stream = new MemoryStream();
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    var fileContent = new ByteArrayContent(stream.ToArray());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    formData.Add(fileContent, "Image", file.FileName);
                }

                var response = await _httpClient.PostAsync($"{BaseUrl}/{endpoint}", formData);
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

        /// <summary>
        /// Обновление студии с изображением
        /// </summary>
        public async Task<(bool Success, ErrorHandlingService.ApiErrorResponse? Error)> PutWithFileAsync(string endpoint, object data, IFormFile? file = null)
        {
            try
            {
                AddAuthorizationHeader();

                using var formData = new MultipartFormDataContent();

                var properties = data.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(data)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        formData.Add(new StringContent(value), prop.Name);
                    }
                }

                if (file != null)
                {
                    using var stream = new MemoryStream();
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    var fileContent = new ByteArrayContent(stream.ToArray());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    formData.Add(fileContent, "Image", file.FileName);
                }

                var response = await _httpClient.PutAsync($"{BaseUrl}/{endpoint}", formData);

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

        public async Task<byte[]?> GetImageAsync(string endpoint)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync($"{BaseUrl}/{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}