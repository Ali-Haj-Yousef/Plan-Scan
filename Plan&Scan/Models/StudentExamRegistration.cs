using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace Plan_Scan.Models
{
    public class StudentExamRegistration
    {
        [Key]
        public int RegId { get; set; }

        [Required]
        [Range(1, 1000000, ErrorMessage = "Student Id must be between 1 and 1000000.")]
        [DisplayName("Student Id")]
        public int StudentId { get; set; }

        [Required]
        [StringLength(100)]
        [DisplayName("Student Name")]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Course { get; set; }

        [Required]
        [RegularExpression("^[EF]$", ErrorMessage = "Lang must be 'E' or 'F'.")]
        [DisplayName("Language")]
        public char Lang { get; set; }

        [Required]
        [StringLength(10)]
        [DisplayName("Room")]
        public string Room { get; set; }

        [Range(1, 100, ErrorMessage = "Seat Number must be between 1 and 100.")]
        [DisplayName("Seat Number")]
        public string SeatNb { get; set; }

        [Required]
        [RegularExpression(@"^(0[1-9]|[12][0-9]|3[01])/(0[1-9]|1[0-2])/\d{4}$", ErrorMessage = "Date must be in format dd/mm/yyyy.")]
        public DateOnly Date { get; set; }

        [Required]
        [RegularExpression(@"^([1-9]|1[0-2]):[0-5][0-9] (AM|PM)$", ErrorMessage = "Time must be in format hh:mm AM/PM.")]
        public string Time { get; set; }

        [Required]
        [StringLength(20)]
        [DisplayName("Exam Code")]
        public string ExamCode { get; set; }
        [Required]
        public List<String> AttributesValues
        {
            get
            {
                return new List<String> { StudentId.ToString(), Name, Lang.ToString(), SeatNb};
            }
        }
    }
}
