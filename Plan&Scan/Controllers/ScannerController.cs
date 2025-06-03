using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Plan_Scan.Data;
using Plan_Scan.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http; // Add this

namespace Plan_Scan.Controllers
{
    public class ScannerController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory; // Use factory for HttpClient

        public ScannerController(
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext db,
            IHttpClientFactory httpClientFactory) // Inject factory
        {
            _webHostEnvironment = webHostEnvironment;
            _context = db;
            _httpClientFactory = httpClientFactory;
        }
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Index(IFormFile? file, CancellationToken requestAborted)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Create a timeout token (e.g., 10 minutes)
            var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                requestAborted,
                timeoutTokenSource.Token
            );

            try
            {
                return await markPresentStudentsInRegistrationsFile(file, linkedTokenSource.Token);
            }
            catch (TaskCanceledException) when (timeoutTokenSource.IsCancellationRequested)
            {
                return StatusCode(408, "Processing timeout. The operation took too long.");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(499, "Request canceled by client.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        public async Task<List<List<string>>> getPresentStudentsFromPdf(
            IFormFile pdfFile,
            CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10); // Set timeout to 10 minutes

            using var form = new MultipartFormDataContent();
            using var stream = pdfFile.OpenReadStream();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            form.Add(fileContent, "file", pdfFile.FileName);

            // Pass cancellation token to PostAsync
            var response = await client.PostAsync(
                "http://localhost:8000/process",
                form,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonString);

            var presencesInDate = new List<List<string>>();
            foreach (var entry in data)
            {
                var identifiers = entry.Key.Split('-');
                foreach (var id in entry.Value)
                {
                    presencesInDate.Add(new List<string>(identifiers) { id });
                }
            }
            return presencesInDate;
        }

        public async Task<IActionResult> markPresentStudentsInRegistrationsFile(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            var presences = await getPresentStudentsFromPdf(file, cancellationToken);

            using var package = new ExcelPackage();
            var registrationsSheet = package.Workbook.Worksheets.Add("RegistrationsSheet");

            var errorsSheet = package.Workbook.Worksheets.Add("ErrorsSheet");

            int regRow = 1, errorRow = 1;

            registrationsSheet.Cells[errorRow, 1].Value = "ID";
            registrationsSheet.Cells[errorRow, 2].Value = "Name";
            registrationsSheet.Cells[errorRow, 3].Value = "Course";
            registrationsSheet.Cells[errorRow, 4].Value = "Lang";
            registrationsSheet.Cells[errorRow, 5].Value = "Room";
            registrationsSheet.Cells[errorRow, 6].Value = "SeatNb";
            registrationsSheet.Cells[errorRow, 7].Value = "Date";
            registrationsSheet.Cells[errorRow, 8].Value = "Time";
            registrationsSheet.Cells[errorRow, 9].Value = "CodeExamDay";

            errorsSheet.Cells[errorRow, 1].Value = "ID";
            errorsSheet.Cells[errorRow, 2].Value = "Name";
            errorsSheet.Cells[errorRow, 3].Value = "Course";
            errorsSheet.Cells[errorRow, 4].Value = "Lang";
            errorsSheet.Cells[errorRow, 5].Value = "Room";
            errorsSheet.Cells[errorRow, 6].Value = "SeatNb";
            errorsSheet.Cells[errorRow, 7].Value = "Date";
            errorsSheet.Cells[errorRow, 8].Value = "Time";
            errorsSheet.Cells[errorRow, 9].Value = "CodeExamDay";

            for (int col = 1; col <= 9; col++)
            {
                errorsSheet.Cells[errorRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                errorsSheet.Cells[errorRow, col].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                registrationsSheet.Cells[errorRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                registrationsSheet.Cells[errorRow, col].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
            }

            errorRow++;
            regRow++;

            foreach (var presence in presences)
            {
                // Parse data from presence list
                var date = DateOnly.ParseExact(presence[0], "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                var room = presence[3];
                var course = presence[2];
                var examCode = Convert.ToInt32(presence[1]);
                var studentId = Convert.ToInt32(presence[4]);

                // Async DB query with cancellation token
                var reg = await _context.StudentExamRegistrations
                    .Where(r =>
                        r.Date == date &&
                        r.Room == room &&
                        r.Course == course &&
                        r.ExamCode == examCode &&
                        r.StudentId == studentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (reg == null)
                {
                    // Add to error sheet
                    errorsSheet.Cells[errorRow, 1].Value = presence[0];
                    errorsSheet.Cells[errorRow, 2].Value = presence[1];
                    errorsSheet.Cells[errorRow, 3].Value = presence[2];
                    errorsSheet.Cells[errorRow, 4].Value = presence[3];
                    errorsSheet.Cells[errorRow, 5].Value = presence[4];
                    errorsSheet.Cells[errorRow, 6].Value = "not existing";
                    errorRow++;
                }
                else
                {
                    // Add to registration sheet
                    registrationsSheet.Cells[regRow, 1].Value = reg.StudentId;
                    registrationsSheet.Cells[regRow, 2].Value = reg.Name;
                    registrationsSheet.Cells[regRow, 3].Value = reg.Course;
                    registrationsSheet.Cells[regRow, 4].Value = reg.Lang.ToString();
                    registrationsSheet.Cells[regRow, 5].Value = reg.Room;
                    registrationsSheet.Cells[regRow, 6].Value = reg.SeatNb;
                    registrationsSheet.Cells[regRow, 7].Value = reg.Date;
                    registrationsSheet.Cells[regRow, 8].Value = reg.Time;
                    registrationsSheet.Cells[regRow, 9].Value = reg.ExamCode;
                    regRow++;
                }
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0; // Reset the stream position

            // Return the file as a download
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Registrations.xlsx");
        }
    }
}