using SIRGA.Application.Services;

namespace SIRGA.Tests;

/// <summary>
/// El usuario puede escribir el importe con coma o con punto. Los dos tienen que valer lo mismo.
/// </summary>
public class ImporteParserTests
{
  [Theory]
  [InlineData("1758785,23", 1758785.23)]
  [InlineData("1758785.23", 1758785.23)]
  [InlineData("1.758.785,23", 1758785.23)]
  [InlineData("1.758.785.23", 175878523)]   // todos puntos: se interpretan como miles
  [InlineData("1.500.000", 1500000)]
  [InlineData("1.500", 1500)]
  [InlineData("1.5", 1.5)]
  [InlineData("2,75", 2.75)]
  [InlineData("1234", 1234)]
  [InlineData("$ 1.758.785,23", 1758785.23)]
  [InlineData("-1500,50", -1500.50)]
  [InlineData("", 0)]
  [InlineData(null, 0)]
  public void Interpreta_importes_con_coma_o_punto(string? texto, decimal esperado)
  {
    var valor = ImporteParser.Parse(texto);
    Assert.NotNull(valor);
    Assert.Equal(esperado, valor!.Value);
  }

  [Fact]
  public void Sin_numeros_devuelve_cero()
  {
    Assert.Equal(0m, ImporteParser.Parse("abc"));
  }
}
