using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using StudioRental_Markov.Data;
using StudioRental_Markov.Models;
using System.Drawing;

namespace StudioRental_Markov.Services
{
    public class ExcelExportService
    {
        private readonly AppDbContext _db;

        public ExcelExportService(AppDbContext db)
        {
            _db = db;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Экспорт списка студий в Excel
        /// </summary>
        public async Task<byte[]> ExportStudiosToExcel()
        {
            var studios = await _db.Studios
                .Include(s => s.Owner)
                .OrderBy(s => s.Id)
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Студии");

            // Заголовки
            worksheet.Cells["A1"].Value = "ID";
            worksheet.Cells["B1"].Value = "Название";
            worksheet.Cells["C1"].Value = "Адрес";
            worksheet.Cells["D1"].Value = "Цена за час";
            worksheet.Cells["E1"].Value = "Владелец";
            worksheet.Cells["F1"].Value = "Email владельца";
            worksheet.Cells["G1"].Value = "Статус";
            worksheet.Cells["H1"].Value = "Дата создания";

            using (var range = worksheet.Cells["A1:H1"])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                range.Style.Font.Color.SetColor(Color.White);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int row = 2;
            foreach (var studio in studios)
            {
                worksheet.Cells[row, 1].Value = studio.Id;
                worksheet.Cells[row, 2].Value = studio.Name;
                worksheet.Cells[row, 3].Value = studio.Address;
                worksheet.Cells[row, 4].Value = studio.PricePerHour;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00 ₽";
                worksheet.Cells[row, 5].Value = studio.Owner?.FullName ?? "-";
                worksheet.Cells[row, 6].Value = studio.Owner?.Email ?? "-";
                worksheet.Cells[row, 7].Value = studio.IsApproved ? "Подтверждена" : "На модерации";
                worksheet.Cells[row, 8].Value = studio.CreatedAt.ToString("dd.MM.yyyy HH:mm");

                row++;
            }

            worksheet.Cells.AutoFitColumns();

            int totalRow = row + 1;
            worksheet.Cells[totalRow, 1].Value = $"Всего студий: {studios.Count}";
            worksheet.Cells[totalRow, 1, totalRow, 4].Merge = true;
            worksheet.Cells[totalRow, 1].Style.Font.Bold = true;
            worksheet.Cells[totalRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[totalRow, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(221, 235, 247));

            return await package.GetAsByteArrayAsync();
        }

        /// <summary>
        /// Экспорт списка бронирований в Excel
        /// </summary>
        public async Task<byte[]> ExportBookingsToExcel(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _db.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Studio)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(b => b.StartTime >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(b => b.EndTime <= endDate.Value);

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Бронирования");


            string[] headers = { "ID", "Клиент", "Телефон клиента", "Студия", "Начало", "Конец",
                                 "Длительность (ч)", "Стоимость", "Статус", "Дата создания" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(46, 125, 50));
                range.Style.Font.Color.SetColor(Color.White);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            decimal totalRevenue = 0;

            foreach (var booking in bookings)
            {
                var duration = (booking.EndTime - booking.StartTime).TotalHours;
                totalRevenue += booking.TotalPrice;

                worksheet.Cells[row, 1].Value = booking.Id;
                worksheet.Cells[row, 2].Value = booking.Customer?.FullName ?? "-";
                worksheet.Cells[row, 3].Value = booking.Customer?.Phone ?? "-";
                worksheet.Cells[row, 4].Value = booking.Studio?.Name ?? "-";
                worksheet.Cells[row, 5].Value = booking.StartTime.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[row, 6].Value = booking.EndTime.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[row, 7].Value = Math.Round(duration, 2);
                worksheet.Cells[row, 8].Value = booking.TotalPrice;
                worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0.00 ₽";

                var statusCell = worksheet.Cells[row, 9];
                statusCell.Value = booking.Status switch
                {
                    "Confirmed" => "Подтверждено",
                    "Cancelled" => "Отменено",
                    _ => "Ожидает"
                };

                switch (booking.Status)
                {
                    case "Confirmed":
                        statusCell.Style.Font.Color.SetColor(Color.Green);
                        break;
                    case "Cancelled":
                        statusCell.Style.Font.Color.SetColor(Color.Red);
                        break;
                    default:
                        statusCell.Style.Font.Color.SetColor(Color.Orange);
                        break;
                }

                worksheet.Cells[row, 10].Value = booking.CreatedAt.ToString("dd.MM.yyyy HH:mm");

                row++;
            }

            worksheet.Cells.AutoFitColumns();

            int summaryRow = row + 1;
            worksheet.Cells[summaryRow, 1].Value = "ИТОГО:";
            worksheet.Cells[summaryRow, 7].Value = $"Общая выручка: {totalRevenue:N2} ₽";
            worksheet.Cells[summaryRow, 7, summaryRow, 8].Merge = true;
            worksheet.Cells[summaryRow, 7].Style.Font.Bold = true;
            worksheet.Cells[summaryRow, 7].Style.Font.Size = 12;

            return await package.GetAsByteArrayAsync();
        }

        /// <summary>
        /// Экспорт списка пользователей в Excel
        /// </summary>
        public async Task<byte[]> ExportUsersToExcel()
        {
            var users = await _db.Users.OrderBy(u => u.Id).ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Пользователи");

            string[] headers = { "ID", "ФИО", "Email", "Телефон", "Роль", "Дата регистрации" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 12;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(103, 58, 183));
                range.Style.Font.Color.SetColor(Color.White);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            foreach (var user in users)
            {
                worksheet.Cells[row, 1].Value = user.Id;
                worksheet.Cells[row, 2].Value = user.FullName;
                worksheet.Cells[row, 3].Value = user.Email;
                worksheet.Cells[row, 4].Value = user.Phone ?? "-";

                var roleCell = worksheet.Cells[row, 5];
                roleCell.Value = user.Role == "Admin" ? "Администратор" : "Пользователь";
                if (user.Role == "Admin")
                {
                    roleCell.Style.Font.Color.SetColor(Color.FromArgb(103, 58, 183));
                    roleCell.Style.Font.Bold = true;
                }

                worksheet.Cells[row, 6].Value = user.CreatedAt.ToString("dd.MM.yyyy");
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        /// <summary>
        /// Экспорт детального отчета по студии
        /// </summary>
        public async Task<byte[]> ExportStudioReport(int studioId)
        {
            var studio = await _db.Studios
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == studioId);

            if (studio == null)
                throw new Exception("Студия не найдена");

            var bookings = await _db.Bookings
                .Include(b => b.Customer)
                .Where(b => b.StudioId == studioId && b.Status == "Confirmed")
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            using var package = new ExcelPackage();

            var infoSheet = package.Workbook.Worksheets.Add("Информация о студии");

            infoSheet.Cells["A1"].Value = "ОТЧЕТ ПО СТУДИИ";
            infoSheet.Cells["A1"].Style.Font.Size = 16;
            infoSheet.Cells["A1"].Style.Font.Bold = true;

            infoSheet.Cells["A3"].Value = "Название:";
            infoSheet.Cells["B3"].Value = studio.Name;
            infoSheet.Cells["A4"].Value = "Адрес:";
            infoSheet.Cells["B4"].Value = studio.Address;
            infoSheet.Cells["A5"].Value = "Цена за час:";
            infoSheet.Cells["B5"].Value = studio.PricePerHour;
            infoSheet.Cells["B5"].Style.Numberformat.Format = "#,##0.00 ₽";
            infoSheet.Cells["A6"].Value = "Владелец:";
            infoSheet.Cells["B6"].Value = studio.Owner?.FullName ?? "-";
            infoSheet.Cells["A7"].Value = "Дата создания:";
            infoSheet.Cells["B7"].Value = studio.CreatedAt.ToString("dd.MM.yyyy");
            infoSheet.Cells["A8"].Value = "Статус:";
            infoSheet.Cells["B8"].Value = studio.IsApproved ? "Подтверждена" : "На модерации";

            var totalBookings = bookings.Count;
            var totalHours = bookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            var totalRevenue = bookings.Sum(b => b.TotalPrice);

            infoSheet.Cells["A10"].Value = "СТАТИСТИКА БРОНИРОВАНИЙ";
            infoSheet.Cells["A10"].Style.Font.Bold = true;
            infoSheet.Cells["A11"].Value = "Всего бронирований:";
            infoSheet.Cells["B11"].Value = totalBookings;
            infoSheet.Cells["A12"].Value = "Общее количество часов:";
            infoSheet.Cells["B12"].Value = Math.Round(totalHours, 2);
            infoSheet.Cells["A13"].Value = "Общая выручка:";
            infoSheet.Cells["B13"].Value = totalRevenue;
            infoSheet.Cells["B13"].Style.Numberformat.Format = "#,##0.00 ₽";
            infoSheet.Cells["A13"].Style.Font.Bold = true;

            var bookingsSheet = package.Workbook.Worksheets.Add("Бронирования");

            bookingsSheet.Cells["A1"].Value = "Дата начала";
            bookingsSheet.Cells["B1"].Value = "Дата окончания";
            bookingsSheet.Cells["C1"].Value = "Длительность (ч)";
            bookingsSheet.Cells["D1"].Value = "Клиент";
            bookingsSheet.Cells["E1"].Value = "Стоимость";

            using (var range = bookingsSheet.Cells["A1:E1"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                range.Style.Font.Color.SetColor(Color.White);
            }

            int row = 2;
            foreach (var booking in bookings)
            {
                var duration = (booking.EndTime - booking.StartTime).TotalHours;
                bookingsSheet.Cells[row, 1].Value = booking.StartTime.ToString("dd.MM.yyyy HH:mm");
                bookingsSheet.Cells[row, 2].Value = booking.EndTime.ToString("dd.MM.yyyy HH:mm");
                bookingsSheet.Cells[row, 3].Value = Math.Round(duration, 2);
                bookingsSheet.Cells[row, 4].Value = booking.Customer?.FullName ?? "-";
                bookingsSheet.Cells[row, 5].Value = booking.TotalPrice;
                bookingsSheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00 ₽";
                row++;
            }

            bookingsSheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }
    }
}