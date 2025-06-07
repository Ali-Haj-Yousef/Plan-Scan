using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Plan_Scan.Models
{
    public class PlanSheetViewModel
    {

        [DisplayName("Exam Code")]
        public string? ExamCode { get; set; }

        public string? Room { get; set; }

        [Required]
        [DisplayName("Start Date")]
        public DateOnly StartDate { get; set; }

        [DisplayName("End Date")]
        public DateOnly? EndDate { get; set; }

        [ValidateNever]
        public List<SelectListItem> ExamCodeList { get; set; }

        [ValidateNever]
        public List<SelectListItem> RoomList { get; set; }
    }
}
