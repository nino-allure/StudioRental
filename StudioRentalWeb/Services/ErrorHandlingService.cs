using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;

namespace StudioRentalWeb.Services
{
    public class ErrorHandlingService
    {
        public class ApiErrorResponse
        {
            public string? Message { get; set; }
            public string? Title { get; set; }
            public int StatusCode { get; set; }
            public Dictionary<string, List<string>>? Errors { get; set; }
        }

        public async Task<ApiErrorResponse> HandleErrorResponse(HttpResponseMessage response)
        {
            var errorResponse = new ApiErrorResponse
            {
                StatusCode = (int)response.StatusCode
            };

            try
            {
                var content = await response.Content.ReadAsStringAsync();

                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        errorResponse.Title = "Ошибка в данных";
                        try
                        {
                            var errors = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(content);
                            if (errors != null && errors.Count > 0)
                            {
                                errorResponse.Errors = errors;
                                errorResponse.Message = string.Join("; ", errors.SelectMany(x => x.Value));
                            }
                            else
                            {
                                errorResponse.Message = content;
                            }
                        }
                        catch
                        {
                            errorResponse.Message = content;
                        }
                        break;

                    case HttpStatusCode.Unauthorized:
                        errorResponse.Title = "Не авторизован";
                        errorResponse.Message = "Пожалуйста, войдите в систему заново";
                        break;

                    case HttpStatusCode.Forbidden:
                        errorResponse.Title = "Доступ запрещен";
                        errorResponse.Message = "У вас недостаточно прав для выполнения этого действия";
                        break;

                    case HttpStatusCode.NotFound:
                        errorResponse.Title = "Не найдено";
                        errorResponse.Message = "Запрашиваемый ресурс не найден";
                        break;

                    case HttpStatusCode.Conflict:
                        errorResponse.Title = "Конфликт данных";
                        errorResponse.Message = content;
                        break;

                    case HttpStatusCode.InternalServerError:
                        errorResponse.Title = "Внутренняя ошибка сервера";
                        errorResponse.Message = "Произошла ошибка на сервере. Попробуйте позже";
                        break;

                    default:
                        errorResponse.Title = "Ошибка";
                        errorResponse.Message = content;
                        break;
                }
            }
            catch
            {
                errorResponse.Message = "Произошла неизвестная ошибка";
            }

            return errorResponse;
        }
    }

    public class NotificationService
    {
        public enum NotificationType
        {
            Success,
            Error,
            Warning,
            Info
        }

        public void AddNotification(Controller controller, string message, NotificationType type)
        {
            var key = type.ToString().ToLower();
            controller.TempData[key] = message;
            controller.TempData[$"{key}_type"] = type.ToString();
        }

        public void AddSuccess(Controller controller, string message)
        {
            AddNotification(controller, message, NotificationType.Success);
        }

        public void AddError(Controller controller, string message)
        {
            AddNotification(controller, message, NotificationType.Error);
        }

        public void AddWarning(Controller controller, string message)
        {
            AddNotification(controller, message, NotificationType.Warning);
        }

        public void AddInfo(Controller controller, string message)
        {
            AddNotification(controller, message, NotificationType.Info);
        }
    }
}