using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

public class Province
{
  public int Id { get; set; }

  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = null!;

  public virtual ICollection<City> Cities { get; set; } = new List<City>();
}

public class City
{
  public int Id { get; set; }

  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = null!;

  [MaxLength(10)]
  public string? ZipCode { get; set; }

  public int ProvinceId { get; set; }
  public virtual Province Province { get; set; } = null!;
}
