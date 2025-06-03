using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Plan_Scan.Models
{
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "Please select a file.")]
        [DisplayName("Select a File")]
        public IFormFile File { get; set; }
    }
}
