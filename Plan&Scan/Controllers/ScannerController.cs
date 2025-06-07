using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
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
        public IActionResult UploadPDF()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadPDF(IFormFile? file, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10); // Set timeout to 10 minutes

            // Add the security token to the client headers
            client.DefaultRequestHeaders.Add("X-Security-Token", "UniversityReaderSecret2023");
            using var form = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            form.Add(fileContent, "file", file.FileName);
            // Step 1: Request job ID
            var jobResponse = await client.PostAsync("http://attendance-py.apps.ul.edu.lb/process", form, cancellationToken);

            // Ensure the response is successful
            //jobResponse.EnsureSuccessStatusCode();

            var jobResponseContent = await jobResponse.Content.ReadAsStringAsync(cancellationToken);

            // Deserialize the JSON response to access job_id
            var jobData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jobResponseContent);

            if (jobData.TryGetValue("job_id", out var jobIdValue))
            {
                string jobeId = jobIdValue.ToString();
                TempData["job-id"] = jobeId;
                return View("DownloadExcelFile", jobeId); // Pass only the job_id to the view
            }
            else
            {
                // Handle the case where job_id is not present
                return View("error"); // Or another appropriate action
            }
        }

        public async Task<List<List<string>>> getPresentStudentsFromPdf(
    Dictionary<string, List<string>> data,
    CancellationToken cancellationToken)
        {
            //var dataDictionary = data as Dictionary<string, List<string>>;

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
            Dictionary<string, List<string>> data,
            CancellationToken cancellationToken)
        {
            var presences = await getPresentStudentsFromPdf(data, cancellationToken);

            //return View("test", presences);

            using var package = new ExcelPackage();
            var registrationsSheet = package.Workbook.Worksheets.Add("RegistrationsSheet");

            int regRow = 1;

            registrationsSheet.Cells[regRow, 1].Value = "ID";
            registrationsSheet.Cells[regRow, 2].Value = "Name";
            registrationsSheet.Cells[regRow, 3].Value = "Course";
            registrationsSheet.Cells[regRow, 4].Value = "Lang";
            registrationsSheet.Cells[regRow, 5].Value = "Room";
            registrationsSheet.Cells[regRow, 6].Value = "SeatNb";
            registrationsSheet.Cells[regRow, 7].Value = "Date";
            registrationsSheet.Cells[regRow, 8].Value = "Time";
            registrationsSheet.Cells[regRow, 9].Value = "CodeExamDay";

            for (int col = 1; col <= 9; col++)
            {
                registrationsSheet.Cells[regRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                registrationsSheet.Cells[regRow, col].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
            }

            regRow++;

            foreach (var presence in presences)
            {
                // Parse data from presence list
                var date = DateOnly.ParseExact(presence[0], "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                var room = presence[3];
                var course = presence[2];
                var examCode = presence[1];
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

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0; // Reset the stream position

            // Return the file as a download
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Registrations.xlsx");
        }

        
        [HttpGet]
        public async Task<IActionResult> DownloadExcelFile(string jobId, CancellationToken cancellationToken)
        {
            var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutTokenSource.Token
            );

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            client.DefaultRequestHeaders.Add("X-Security-Token", "UniversityReaderSecret2023");

            var jobResponse = await client.GetAsync($"http://attendance-py.apps.ul.edu.lb/job/{jobId}", cancellationToken);
            var jobResponseContent = await jobResponse.Content.ReadAsStringAsync(cancellationToken);

            var jobData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jobResponseContent);
            while (jobData.GetValueOrDefault("status").Equals("processing"))
            {
                jobResponse = await client.GetAsync($"http://attendance-py.apps.ul.edu.lb/job/{jobId}", cancellationToken);
                jobResponseContent = await jobResponse.Content.ReadAsStringAsync(cancellationToken);
                jobData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jobResponseContent);
            }
            //return View("test", jobData.GetValueOrDefault("data").ToString());
            var jsonData = jobData.GetValueOrDefault("data").ToString();
            var data = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonData);
            return await markPresentStudentsInRegistrationsFile(data, linkedTokenSource.Token);
        
        
    }
    }
}