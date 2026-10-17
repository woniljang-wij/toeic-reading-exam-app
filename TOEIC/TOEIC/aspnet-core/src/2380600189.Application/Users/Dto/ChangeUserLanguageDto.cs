using System.ComponentModel.DataAnnotations;

namespace TOEIC.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}