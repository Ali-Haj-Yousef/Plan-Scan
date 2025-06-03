using System.Diagnostics;
using System.Reflection.Metadata;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml.ConditionalFormatting;
using Org.BouncyCastle.Utilities;
using Plan_Scan.Data;
using Plan_Scan.Models;
using Document = iTextSharp.text.Document;


namespace Plan_Scan.Controllers
{
    public class PlanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private BaseFont ocrbBaseFont; // Declare OCR-B font
        private BaseFont arabicBaseFont; // Declare Arabic font
        private Font arabicFont;
        private Font ocrbFont;
        Font regularFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
        Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

        public PlanController(ApplicationDbContext context)
        {
            _context = context;            
        }

        private List<DateOnly> GetDateRange(DateOnly startDate, DateOnly? endDate)
        {
            List<DateOnly> dates = new List<DateOnly>();

            for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dates.Add(date);
            }

            return dates;
        }

        private PdfPTable CreateTopHeader(DateOnly dateOnly, string time)
        {
            // Define the font path relative to the wwwroot directory
            string fontPath = Path.Combine("wwwroot", "fonts", "ocr-b-regular.ttf"); // Adjust the filename if necessary

            // Path for Arial font
            string arialFontPath = Path.Combine("wwwroot", "fonts", "arial.ttf");
            BaseFont arabicBaseFont = BaseFont.CreateFont(
                arialFontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font arabicFont = new Font(arabicBaseFont, 12, Font.NORMAL, BaseColor.BLACK);

            // Path for Arial Bold font
            string arialBoldFontPath = Path.Combine("wwwroot", "fonts", "arialbd.ttf");
            BaseFont arabicBoldBaseFont = BaseFont.CreateFont(
                arialBoldFontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font arabicBoldFont = new Font(arabicBoldBaseFont, 12, Font.NORMAL, BaseColor.BLACK);

            // Path for OCR-B font
            BaseFont ocrbBaseFont = BaseFont.CreateFont(
                fontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font ocrbFont = new Font(ocrbBaseFont, 10, Font.NORMAL, BaseColor.BLACK);

            // Create a table for the header
            PdfPTable header = new PdfPTable(3);
            header.WidthPercentage = 100;

            // Left cell
            PdfPCell LU = new PdfPCell(new Phrase("Lebanese University", arabicBoldFont));
            LU.Border = PdfPCell.NO_BORDER;
            LU.HorizontalAlignment = Element.ALIGN_LEFT;
            header.AddCell(LU);

            // Center cell
            PdfPCell title = new PdfPCell(new Phrase("توزيع الطلاب على المقاعد", arabicBoldFont));
            title.Border = PdfPCell.NO_BORDER;
            title.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
            title.HorizontalAlignment = Element.ALIGN_CENTER;
            header.AddCell(title);


            // Right cell
            PdfPCell universityTitle = new PdfPCell(new Phrase("الجامعة اللبنانية", arabicBoldFont));
            universityTitle.Border = PdfPCell.NO_BORDER;
            universityTitle.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
            universityTitle.HorizontalAlignment = Element.ALIGN_LEFT;
            header.AddCell(universityTitle);

            PdfPCell FS = new PdfPCell(new Phrase("Faculty of Science (FS1)", arabicFont));
            FS.Border = PdfPCell.NO_BORDER;
            FS.HorizontalAlignment = Element.ALIGN_LEFT;
            header.AddCell(FS);


            PdfPCell dateField = new PdfPCell(new Phrase(dateOnly.ToString() + " - " + time, ocrbFont));
            dateField.Border = PdfPCell.NO_BORDER;
            dateField.HorizontalAlignment = Element.ALIGN_CENTER;
            header.AddCell(dateField);

            PdfPCell facultyTitle = new PdfPCell(new Phrase("كلية العلوم - الفرع الأول", arabicFont));
            facultyTitle.Border = PdfPCell.NO_BORDER;
            facultyTitle.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
            facultyTitle.HorizontalAlignment = Element.ALIGN_LEFT;
            header.AddCell(facultyTitle);

            return header;
        }

        private PdfPTable CreateIdentifiersHeader(String room, String course, int examCode)
        {
            string fontPath = Path.Combine("wwwroot", "fonts", "ocr-b-regular.ttf"); // Adjust the filename if necessary

            // Path for Arial font
            string arialFontPath = Path.Combine("wwwroot", "fonts", "arial.ttf");
            BaseFont arabicBaseFont = BaseFont.CreateFont(
                arialFontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font arabicFont = new Font(arabicBaseFont, 12, Font.NORMAL, BaseColor.BLACK);

            // Path for OCR-B font
            BaseFont ocrbBaseFont = BaseFont.CreateFont(
                fontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font ocrbFont = new Font(ocrbBaseFont, 14, Font.NORMAL, BaseColor.BLACK);

            PdfPTable identifiersHeader = new PdfPTable(3);
            identifiersHeader.WidthPercentage = 100;

            // Exam Code: 8 - 10
            Chunk examCodeText = new Chunk("Exam Code: ", arabicFont);
            Chunk examCodeValue = new Chunk(examCode.ToString(), ocrbFont);
            Phrase examCodeCellContent = new Phrase();
            examCodeCellContent.Add(examCodeText);
            examCodeCellContent.Add(examCodeValue);
            PdfPCell examCodeCell = new PdfPCell(examCodeCellContent)
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT
            };
            identifiersHeader.AddCell(examCodeCell);

            // Course: I3300
            Chunk courseText = new Chunk("Course: ", arabicFont);
            Chunk courseValue = new Chunk(course, ocrbFont);
            Phrase courseCellContent = new Phrase();
            courseCellContent.Add(courseText);
            courseCellContent.Add(courseValue);
            PdfPCell courseCell = new PdfPCell(courseCellContent)
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            identifiersHeader.AddCell(courseCell);

            Chunk roomText = new Chunk("Room: ", arabicFont);
            Chunk roomValue = new Chunk(room, ocrbFont);
            Phrase roomCellContent = new Phrase();
            roomCellContent.Add(roomText);
            roomCellContent.Add(roomValue);
            PdfPCell roomCell = new PdfPCell(roomCellContent)
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };
            identifiersHeader.AddCell(roomCell);
            
            return identifiersHeader;
        }

        private PdfPTable CreateRegistrationsTable(List<StudentExamRegistration> studentExamRegistrations)
        {

            PdfPTable registrationsTable = new PdfPTable(5);
            registrationsTable.WidthPercentage = 80;
            float[] columnWidths = new float[] { 25, 40, 10, 15, 20 }; // Set widths for each column
            registrationsTable.SetWidths(columnWidths);

            string fontPath = Path.Combine("wwwroot", "fonts", "ocr-b-regular.ttf"); // Adjust the filename if necessary

            // Path for Arial font
            string arialFontPath = Path.Combine("wwwroot", "fonts", "arial.ttf");
            BaseFont arabicBaseFont = BaseFont.CreateFont(
                arialFontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font arabicFont = new Font(arabicBaseFont, 12, Font.NORMAL, BaseColor.BLACK);

            // Path for OCR-B font
            BaseFont ocrbBaseFont = BaseFont.CreateFont(
                fontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font ocrbFont = new Font(ocrbBaseFont, 14, Font.NORMAL, BaseColor.BLACK);
            
            string arialBoldFontPath = Path.Combine("wwwroot", "fonts", "arialbd.ttf");
            BaseFont arabicBoldBaseFont = BaseFont.CreateFont(
                arialBoldFontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font arabicBoldFont = new Font(arabicBoldBaseFont, 11, Font.NORMAL, BaseColor.BLACK);
            // Configure header font (optional, keep existing or change as needed)
            Font headerFont = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD);
            
            //registrationsTable.AddCell(new PdfPCell() { BorderWidth = 1f});
            
            // Add headers
            string[] headers = { "Student ID", "Name", "Lang", "Seat", "Presence" };
            for(int i = 0; i < headers.Length; i++)
            {
                PdfPCell headerCell = new PdfPCell(new Phrase(headers[i], arabicBoldFont));
                headerCell.Padding = 5;
                headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                headerCell.VerticalAlignment = Element.ALIGN_CENTER;
                headerCell.BorderWidth = 1f;
                registrationsTable.AddCell(headerCell);
            }

            // Add student data
            for(int i = 0; i < studentExamRegistrations.Count(); i++)
            {
                for (int j = 0; j < studentExamRegistrations[i].AttributesValues.Count(); j++)
                {
                    PdfPCell cell;
                    if (j == 1)
                        cell = new PdfPCell(new Phrase(studentExamRegistrations[i].AttributesValues[j], arabicFont)); // Use OCR-B font
                    else
                        cell = new PdfPCell(new Phrase(studentExamRegistrations[i].AttributesValues[j], ocrbFont));

                    cell.Padding = 14;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.BorderWidth = 1f;

                    // Enable RTL for Arabic text cells
                    if (studentExamRegistrations[i].AttributesValues[j].Any(c => c >= 0x0600 && c <= 0x06FF)) // Check if text contains Arabic characters
                    {
                        cell.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                        cell.HorizontalAlignment = Element.ALIGN_LEFT;
                    }

                    registrationsTable.AddCell(cell);
                }
                registrationsTable.AddCell(new PdfPCell() { BorderWidth = 1f}); // Empty presence cell
            }

            return registrationsTable;
        }

        public PdfPTable CreateCountingTable(int regNb)
        {
            string arialBoldFontPath = Path.Combine("wwwroot", "fonts", "arialbd.ttf");
            BaseFont arabicBoldBaseFont = BaseFont.CreateFont(
                arialBoldFontPath,
                BaseFont.IDENTITY_H,
                BaseFont.EMBEDDED
            );
            Font arabicBoldFont = new Font(arabicBoldBaseFont, 12, Font.NORMAL, BaseColor.BLACK);

            PdfPTable counterTable = new PdfPTable(3);
            counterTable.WidthPercentage = 80;

            PdfPCell absenceField = new PdfPCell(new Phrase("الغياب: ", arabicBoldFont));
            absenceField.Border = PdfPCell.NO_BORDER;
            absenceField.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
            absenceField.HorizontalAlignment = Element.ALIGN_LEFT;
            counterTable.AddCell(absenceField);

            PdfPCell presenceField = new PdfPCell(new Phrase("الحضور: ", arabicBoldFont));
            presenceField.Border = PdfPCell.NO_BORDER;
            presenceField.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
            presenceField.HorizontalAlignment = Element.ALIGN_LEFT;
            counterTable.AddCell(presenceField);

            PdfPCell regTotal = new PdfPCell(new Phrase("الاجمالي: " + regNb, arabicBoldFont));
            regTotal.Border = PdfPCell.NO_BORDER;
            regTotal.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
            regTotal.HorizontalAlignment = Element.ALIGN_LEFT;
            counterTable.AddCell(regTotal);

            return counterTable;
        }

        [HttpGet]
        public JsonResult GetStartDateOptions(int? examCode, string? room, string? endDate)
        {
            var query = _context.StudentExamRegistrations.AsQueryable();

            // Add condition for examCode
            if (examCode.HasValue)
            {
                query = query.Where(x => x.ExamCode == examCode.Value);
            }

            // Add condition for room if it's provided
            if (!string.IsNullOrEmpty(room))
            {
                query = query.Where(x => x.Room == room);
            }

            // Add condition for endDate if it's provided
            if (!string.IsNullOrEmpty(endDate))
            {
                if (DateOnly.TryParse(endDate, out DateOnly end))
                {
                    query = query.Where(x => x.Date < end); // Assuming a DateOnly property exists
                }
            }

            var options = query
                .Select(x => new
                {
                    value = x.Date,
                    text = x.Date
                })
                .Distinct()
                .ToList();

            return Json(options);
        }

        [HttpGet]
        public JsonResult GetEndDateOptions(int? examCode, string? room, string? startDate)
        {
            var query = _context.StudentExamRegistrations.AsQueryable();

            // Add condition for examCode
            if (examCode.HasValue)
            {
                query = query.Where(x => x.ExamCode == examCode.Value);
            }

            // Add condition for room if it's provided
            if (!string.IsNullOrEmpty(room))
            {
                query = query.Where(x => x.Room == room);
            }

            // Add condition for endDate if it's provided
            if (!string.IsNullOrEmpty(startDate))
            {
                if (DateOnly.TryParse(startDate, out DateOnly end))
                {
                    query = query.Where(x => x.Date > end); // Assuming a DateOnly property exists
                }
            }

            var options = query
                .Select(x => new
                {
                    value = x.Date,
                    text = x.Date
                })
                .Distinct()
                .ToList();

            return Json(options);
        }

        [HttpGet]
        public JsonResult GetRoomOptions(int? examCode, string? startDate, string? endDate)
        {
            var query = _context.StudentExamRegistrations.AsQueryable();

            // Add condition for examCode
            if (examCode.HasValue)
            {
                query = query.Where(x => x.ExamCode == examCode.Value);
            }

            // Add condition for startDate if it's provided
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (DateOnly.TryParse(startDate, out DateOnly start) && DateOnly.TryParse(endDate, out DateOnly end))
                {
                    query = query.Where(x => x.Date >= start && x.Date <= end); // Assuming a DateOnly property exists
                }
            }
            else if (!string.IsNullOrEmpty(startDate))
            {
                if (DateOnly.TryParse(startDate, out DateOnly start))
                {
                    query = query.Where(x => x.Date == start); // Assuming a DateOnly property exists
                }
            }
            else if (!string.IsNullOrEmpty(endDate))
            {
                if (DateOnly.TryParse(endDate, out DateOnly end))
                {
                    query = query.Where(x => x.Date <= end); // Assuming a DateOnly property exists
                }
            }

            var options = query
                .Select(x => new
                {
                    value = x.Room,
                    text = x.Room
                })
                .Distinct()
                .ToList();

            return Json(options);
        }

        [HttpGet]
        public JsonResult GetExamCodeOptions(string? room, string? startDate, string? endDate)
        {
            var query = _context.StudentExamRegistrations.AsQueryable();

            // Add condition for room
            if (!string.IsNullOrEmpty(room))
            {
                query = query.Where(x => x.Room == room);
            }

            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (DateOnly.TryParse(startDate, out DateOnly start) && DateOnly.TryParse(endDate, out DateOnly end))
                {
                    query = query.Where(x => x.Date >= start && x.Date <= end); // Assuming a DateOnly property exists
                }
            }
            else if (!string.IsNullOrEmpty(startDate))
            {
                if (DateOnly.TryParse(startDate, out DateOnly start))
                {
                    query = query.Where(x => x.Date == start); // Assuming a DateOnly property exists
                }
            }
            else if (!string.IsNullOrEmpty(endDate))
            {
                if (DateOnly.TryParse(endDate, out DateOnly end))
                {
                    query = query.Where(x => x.Date <= end); // Assuming a DateOnly property exists
                }
            }

            var options = query
                .Select(x => new
                {
                    value = x.ExamCode,
                    text = x.ExamCode
                })
                .Distinct()
                .ToList();

            return Json(options);
        }

        public IActionResult GeneratePDF()
        {
            PlanSheetViewModel planSheetViewModel = new()
            {
                ExamCodeList = _context.StudentExamRegistrations.Select(r => new SelectListItem
                {
                    Text = r.ExamCode.ToString(),
                }).Distinct().ToList(),
               
                RoomList = _context.StudentExamRegistrations.Select(r => new SelectListItem
                {
                    Text = r.Room,
                }).Distinct().ToList()
            };
            
            return View();
        }

        [HttpPost]
        public IActionResult GeneratePDF(PlanSheetViewModel planSheetViewModel)
        {

            List<DateOnly> datesInData = _context.StudentExamRegistrations.Select(r => r.Date).Distinct().ToList();
            
            if (!datesInData.Contains(planSheetViewModel.StartDate))
                ModelState.AddModelError("startDate", "This date doesn't exist.");
            
            List<String> roomsInData = _context.StudentExamRegistrations.Select(r => r.Room).Distinct().ToList();

            if (planSheetViewModel.Room != null && !roomsInData.Contains(planSheetViewModel.Room))
                ModelState.AddModelError("room", "This room doesn't exist.");

            var examCodesInData = _context.StudentExamRegistrations.Select(r => r.ExamCode).Distinct().ToList();

            if (planSheetViewModel.ExamCode != null && !examCodesInData.Contains((int)planSheetViewModel.ExamCode))
                ModelState.AddModelError("examCode", "This exam code doesn't exist.");

            if (!ModelState.IsValid)
            {
                planSheetViewModel.RoomList = _context.StudentExamRegistrations.Select(r => new SelectListItem
                {
                    Text = r.Room
                }).Distinct().ToList();

                planSheetViewModel.ExamCodeList = _context.StudentExamRegistrations.Select(r => new SelectListItem
                {
                    Text = r.ExamCode.ToString()
                }).Distinct().ToList();

                return View();
            }
            else
            {

                using (MemoryStream ms = new MemoryStream())
                {
                    Document document = new Document(PageSize.A4);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    
                    document.Open();

                    PdfContentByte cb = writer.DirectContent;

                    if (planSheetViewModel.EndDate == null)
                        planSheetViewModel.EndDate = planSheetViewModel.StartDate;

                    List<DateOnly> datesRange = GetDateRange(planSheetViewModel.StartDate, planSheetViewModel.EndDate);

                    foreach (DateOnly date in datesRange)
                    {
                        if (datesInData.Contains(date))
                        {
                            IQueryable<StudentExamRegistration> query = _context.StudentExamRegistrations.Where(reg => reg.Date == date);

                            if (planSheetViewModel.ExamCode != null)
                                query = query.Where(reg => reg.ExamCode == planSheetViewModel.ExamCode);

                            if (planSheetViewModel.Room != null)
                                query = query.Where(reg => reg.Room == planSheetViewModel.Room);
                            List<StudentExamRegistration> studentExamRegistrations = query.ToList();
                            var examCodes = studentExamRegistrations.Select(r => r.ExamCode).Distinct().ToList();

                            foreach (var examCode in examCodes)
                            {
                                List<String> courses = studentExamRegistrations.Where(r => r.ExamCode == examCode).Select(r => r.Course).Distinct().ToList();
                                foreach (String course in courses)
                                {
                                    List<String> rooms = studentExamRegistrations.Where(r => r.Course == course && r.ExamCode == examCode).Select(r => r.Room).Distinct().ToList();
                                    foreach (String room in rooms)
                                    {
                                        List<char> languages = studentExamRegistrations.Where(r => r.Room == room && r.Course == course && r.ExamCode == examCode).Select(r => r.Lang).Distinct().ToList();
                                        foreach (var lang in languages)
                                        {
                                            List<StudentExamRegistration> registrations = _context.StudentExamRegistrations
                                                                                            .Where(reg => reg.ExamCode == examCode &&
                                                                                                          reg.Course == course &&
                                                                                                          reg.Room == room && 
                                                                                                          reg.Lang == lang)
                                                                                            .OrderBy(r => r.SeatNb)
                                                                                            .ToList();
                                            int regNb = registrations.Count();

                                            int pagesNb = (int)Math.Ceiling((float)regNb / 15);
                                            
                                            if (regNb % 15 == 0)
                                                pagesNb++;

                                            DateOnly registrationsDate = registrations.First().Date;
                                            string registrationsTime = registrations.First().Time;

                                            for (int i = 1; i <= pagesNb; i++)
                                            {
                                                document.NewPage();

                                                document.Add(CreateTopHeader(registrationsDate, registrationsTime));
                                                
                                                document.Add(new Paragraph("\n"));

                                                document.Add(CreateIdentifiersHeader(room, course, examCode));

                                                if (i == pagesNb && regNb % 15 == 0)
                                                {
                                                    ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER, new Phrase("Page  " + i + " / " + pagesNb, regularFont), document.PageSize.Width / 2, document.Bottom - 20, 0);
                                                    break;
                                                }
                                                
                                                // Add some spacing
                                                document.Add(new Paragraph("\n"));

                                                document.Add(CreateRegistrationsTable(registrations.Take(15).ToList()));
                                                
                                                ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER, new Phrase("Page  " + i + " / " + pagesNb, regularFont), document.PageSize.Width / 2, document.Bottom - 20, 0);

                                                if (i != pagesNb)
                                                    registrations.RemoveRange(0, 15);

                                            }
                                            document.Add(new Paragraph("\n"));
                                            document.Add(CreateCountingTable(regNb));
                                        }
                                    }
                                }
                            }
                        }

                    }

                    // Add header table to the document

                    document.Close();
                    writer.Close();
                    var constant = ms.ToArray();
                    return File(constant, "application/vnd", "firstPdf.pdf");

                }
            }
            
        }
    }

        
    
}
