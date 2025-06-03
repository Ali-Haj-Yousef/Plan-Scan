using Microsoft.EntityFrameworkCore;
using Plan_Scan.Models;

namespace Plan_Scan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }
        public DbSet<StudentExamRegistration> StudentExamRegistrations { get; set; }
    }
}
