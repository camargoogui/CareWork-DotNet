using System.ComponentModel.DataAnnotations;

namespace CareWork.API.Models.DTOs;

public class DeleteAccountDto
{
    [Required(ErrorMessage = "Password is required to confirm account deletion")]
    public string Password { get; set; } = string.Empty;
}

