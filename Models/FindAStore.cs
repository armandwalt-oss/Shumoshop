using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Store
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(300)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(50)]
        public string State { get; set; }

        [StringLength(20)]
        public string ZipCode { get; set; }

        [Phone]
        [StringLength(20)]
        public string Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(500)]
        public string Hours { get; set; }

        public bool IsActive { get; set; } = true;
    }
}