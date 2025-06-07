using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using Plan_Scan.Data;
using Plan_Scan.Models;

namespace Plan_Scan.Controllers
{
    public class UploadController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;
        public UploadController(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }
        public List<StudentExamRegistration> ReadExcelData(IFormFile? uploadFile)
        {
            var registrations = new List<StudentExamRegistration>();

            using (var package = new ExcelPackage(uploadFile.OpenReadStream()))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Get the first worksheet
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Assuming the first row is headers
                {
                    var registration = new StudentExamRegistration
                    {
                        StudentId = int.Parse(worksheet.Cells[row, 1].Text),
                        Name = worksheet.Cells[row, 2].Text,
                        Course = worksheet.Cells[row, 3].Text,
                        Lang = char.Parse(worksheet.Cells[row, 4].Text),
                        Room = worksheet.Cells[row, 5].Text,
                        SeatNb = worksheet.Cells[row, 6].Text,
                        Date = DateOnly.Parse(worksheet.Cells[row, 7].Text),
                        Time = worksheet.Cells[row, 8].Text,
                        ExamCode = worksheet.Cells[row, 9].Text,
                        // Map other properties accordingly
                    };
                    registrations.Add(registration);
                }
            }

            return registrations;
        }
        public void InsertDataToDatabase(IFormFile? uploadFile)
        {
            var registrations = ReadExcelData(uploadFile);
            foreach (var registration in registrations)
            {
                // Add the registration to the context
                _context.StudentExamRegistrations.Add(registration);
            }

            // Save changes to the database
            _context.SaveChanges();
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(FileUploadViewModel? fileUploadViewModel)
        {
            // Check if the file is uploaded
            if (fileUploadViewModel?.File != null)
            {
                var extension = Path.GetExtension(fileUploadViewModel.File.FileName);
                if (extension != ".xls" && extension != ".xlsx")
                {
                    ModelState.AddModelError("File", "The file must be an Excel file (.xls or .xlsx).");
                }
            }

            // If ModelState is valid, proceed with the database operations
            if (ModelState.IsValid)
            {
                if (await _context.StudentExamRegistrations.AnyAsync())
                {
                    _context.StudentExamRegistrations.RemoveRange(_context.StudentExamRegistrations);
                    await _context.SaveChangesAsync();
                }
                InsertDataToDatabase(fileUploadViewModel.File);
                
                TempData["success"] = "Excel file is uploaded successfully";
                return RedirectToAction("Index");
            }

            // Return the view with the current ModelState if validation fails
            return View();
        }
    }
}
