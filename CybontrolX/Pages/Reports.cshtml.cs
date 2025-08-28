using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CybontrolX.DataBase;
using CybontrolX.DBModels;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text;
using iTextSharp.text.pdf;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeOpenXml;
using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using iTextDocument = iTextSharp.text.Document;
using OpenXmlDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using DocumentFormat.OpenXml.Bibliography;
using Word = DocumentFormat.OpenXml.Wordprocessing;
using iText = iTextSharp.text;

namespace CybontrolX.Pages
{
    [Authorize(Roles = "Manager")]
    public class ReportsModel : PageModel
    {
        private readonly AppDbContext _context;

        public ReportsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string SelectedReportType { get; set; } = "EmployeeWorkReport";

        [BindProperty]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-7).Date;

        [BindProperty]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.Date;

        public async Task<IActionResult> OnPostDownloadXls()
        {
            return await GenerateReport("xls", "application/vnd.ms-excel");
        }

        private async Task<IActionResult> GenerateReport(string format, string contentType)
        {
            if (string.IsNullOrEmpty(SelectedReportType))
            {
                ModelState.AddModelError(string.Empty, "Выберите тип отчета.");
                return Page();
            }

            string fileName = $"{SelectedReportType}_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.{format}";
            byte[] fileContent = await GenerateFileContent(format);

            return File(fileContent, contentType, fileName);
        }

        private async Task<byte[]> GenerateFileContent(string format)
        {
            var data = await GetReportData();

            if (data == null || !data.Any())
            {
                var message = $"Нет данных для отчета '{SelectedReportType}' " +
                             $"за период с {StartDate:dd.MM.yyyy} по {EndDate:dd.MM.yyyy}";
                return Encoding.UTF8.GetBytes(message);
            }

            return format switch
            {
                "xls" => GenerateXlsReport(data),
                _ => Encoding.UTF8.GetBytes("Неверный формат")
            };
        }

        private async Task<List<Dictionary<string, object>>> GetReportData()
        {
            return SelectedReportType switch
            {
                "EmployeeWorkReport" => await GenerateEmployeeWorkReport(),
                _ => throw new ArgumentException("Неизвестный тип отчета")
            };
        }

        private async Task<List<Dictionary<string, object>>> GenerateEmployeeWorkReport()
        {
            var reportData = new List<Dictionary<string, object>>();

            var employees = await _context.Employees
                .Include(e => e.DutySchedule)
                .ToListAsync();

            foreach (var employee in employees)
            {
                decimal totalSalaryForPeriod = 0;
                decimal tax = 0;

                decimal hourlyRate = employee.Role == Role.Manager ? 300 : 200;

                var dutyDays = await _context.DutySchedules
                    .Where(d => d.EmployeeId == employee.Id &&
                                d.DutyDate >= StartDate &&
                                d.DutyDate < EndDate.Date.AddDays(1))
                    .ToListAsync();

                foreach (var dutyDay in dutyDays)
                {
                    var payments = await _context.Payments
                        .Where(p => p.EmployeeId == employee.Id &&
                                    p.PaymentDateTime.Date == dutyDay.DutyDate.Date &&
                                    p.Status == "Succeeded")
                        .ToListAsync();

                    decimal totalSales = payments.Sum(p => p.Amount);

                    TimeSpan workDuration = dutyDay.ShiftEnd - dutyDay.ShiftStart;
                    double workHours = workDuration.TotalHours;

                    if (workHours > 4)
                    {
                        workHours -= 1;
                    }

                    decimal salary = ((decimal)workHours * hourlyRate) + (totalSales * 0.1m);

                    totalSalaryForPeriod += salary;
                    tax = totalSalaryForPeriod * 0.13m;

                    var row = new Dictionary<string, object>
                    {
                        { "Дата", dutyDay.DutyDate.ToString("dd.MM.yyyy") },
                        { "Сотрудник", $"{employee.Surname} {employee.Name} {employee.Patronymic}" },
                        { "Начало смены", dutyDay.ShiftStart.ToString(@"hh\:mm") },
                        { "Конец смены", dutyDay.ShiftEnd.ToString(@"hh\:mm") },
                        { "Часов работы", Math.Round(workHours, 2) },
                        { "Сумма продаж", totalSales },
                        { "10% от продаж", totalSales * 0.1m },
                        { "Зарплата", salary },
                        { "Налог 13%", " " },
                        { "Зарплата за период", " " },
                        { "Зарплата/час", hourlyRate },
                        { "Должность", employee.Role == Role.Manager ? "Управляющий" : "Администратор" }
                    };

                    reportData.Add(row);
                }

                var summaryRow = new Dictionary<string, object>
                {
                    { "Дата", "ИТОГО" },
                    { "Сотрудник", $"{employee.Surname} {employee.Name} {employee.Patronymic}" },
                    { "Начало смены", " " },
                    { "Конец смены", " " },
                    { "Часов работы", " " },
                    { "Сумма продаж", " " },
                    { "10% от продаж", " " },
                    { "Зарплата", " " },
                    { "Налог 13%", tax },
                    { "Зарплата за период", totalSalaryForPeriod - tax },
                    { "Зарплата/час", " " },
                    { "Должность", employee.Role == Role.Manager ? "Управляющий" : "Администратор" }
                };

                reportData.Add(summaryRow);
            }

            return reportData;
        }

        private byte[] GenerateXlsReport(List<Dictionary<string, object>> data)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Отчет");

            var headers = new List<string>
            {
                "Дата",
                "Сотрудник",
                "Начало смены",
                "Конец смены",
                "Часов работы",
                "Сумма продаж",
                "10% от продаж",
                "Зарплата",
                "Налог 13%",
                "Зарплата за период",
                "Зарплата/час",
                "Должность"
            };

            int col = 1;
            foreach (var header in headers)
            {
                worksheet.Cells[1, col].Value = header;
                worksheet.Cells[1, col].Style.Font.Bold = true;
                col++;
            }

            for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
            {
                var row = data[rowIndex];
                int colIndex = 1;
                foreach (var key in headers)
                {
                    worksheet.Cells[rowIndex + 2, colIndex].Value = row.ContainsKey(key) ? row[key] : "-";
                    colIndex++;
                }
            }

            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}