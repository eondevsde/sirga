using SIRGA.Application.Services;

namespace SIRGA.Tests;

public class CompanyValidationTests
{
  [Theory]
  [InlineData("23-29037905-9", "23290379059")]
  [InlineData(" 23-29037905-9 ", "23290379059")]
  [InlineData("23290379059", "23290379059")]
  [InlineData("23 29037905 9", "23290379059")]
  [InlineData("29037905", "00029037905")]
  [InlineData("", "")]
  [InlineData(null, "")]
  [InlineData("abc", "")]
  public void NormalizeCuit_ShouldProduce_11Digits(string? input, string expected)
  {
    Assert.Equal(expected, CompanyValidationService.NormalizeCuit(input));
  }

  [Theory]
  [InlineData("23-29037905-9")]
  [InlineData("27287332780")]
  [InlineData("20123456786")]
  [InlineData("30123456781")]
  public void ValidateCuit_ShouldAccept_ValidCuits(string cuit)
  {
    var (ok, error) = CompanyValidationService.ValidateCuit(cuit);
    Assert.True(ok, $"Se esperaba válido: {error}");
    Assert.Null(error);
  }

  [Theory]
  [InlineData("30-12345678-9", "dígito verificador")]   // dv correcto es 1
  [InlineData("27287332789", "dígito verificador")]
  [InlineData("20202020201", "dígito verificador")]
  [InlineData("2012345678", "11 dígitos")]              // corto
  [InlineData("12345678901", "prefijo")]                // prefijo inválido
  [InlineData("9-99999999-9", "11 dígitos")]             // solo 10 dígitos
  [InlineData("", "11 dígitos")]
  [InlineData(null, "11 dígitos")]
  public void ValidateCuit_ShouldReject_InvalidCuits(string? cuit, string reasonFragment)
  {
    var (ok, error) = CompanyValidationService.ValidateCuit(cuit);
    Assert.False(ok);
    Assert.Contains(reasonFragment, error!, StringComparison.OrdinalIgnoreCase);
  }

  [Theory]
  [InlineData("  Empresa   S.A.  ", "EMPRESA S.A.")]
  [InlineData("empresa sa", "EMPRESA SA")]
  [InlineData("  ", "")]
  [InlineData(null, "")]
  public void NormalizeText_ShouldNormalize(string? input, string expected)
  {
    Assert.Equal(expected, CompanyValidationService.NormalizeText(input));
  }

  [Theory]
  [InlineData("011-5555-1234", "01155551234")]
  [InlineData("11 5555 1234", "1155551234")]
  [InlineData(null, "")]
  public void NormalizePhone_ShouldKeepOnlyDigits(string? input, string expected)
  {
    Assert.Equal(expected, CompanyValidationService.NormalizePhone(input));
  }

  [Theory]
  [InlineData("23290379059", "23-29037905-9")]
  [InlineData("30123456781", "30-12345678-1")]
  public void FormatCuit_ShouldAdd_Dashes(string input, string expected)
  {
    Assert.Equal(expected, CompanyValidationService.FormatCuit(input));
  }
}