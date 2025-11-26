using System.ComponentModel.DataAnnotations.Schema;

namespace HomeNetCore.Models
{
    
        public class UserEntity
        {
            [Column("id")]
            public int Id { get; set; }

            [Column("first_name")]
            public string FirstName { get; set; } = string.Empty;

            [Column("last_name")]
            public string LastName { get; set; } = string.Empty;

            [Column("phone_number")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Column("email")]
            public string Email { get; set; } = string.Empty;

            [Column("password")]
            public string Password { get; set; } = string.Empty;

            [Column("created_at")]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}";

        public string DisplayInfo => $"ID: {Id} - {FullName}";
    }

   
}
