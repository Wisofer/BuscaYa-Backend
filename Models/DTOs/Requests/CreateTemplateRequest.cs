namespace BuscaYa.Models.DTOs.Requests;

public class CreateTemplateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Name { get; set; }
}
