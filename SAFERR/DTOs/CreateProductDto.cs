namespace SAFERR.DTOs;

public class CreateProductDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Identifier { get; set; }
    public Guid BrandId { get; set; }
}

