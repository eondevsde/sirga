using SIRGA.Application.Interfaces;
using SIRGA.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace SIRGA.Application.Services;

public class DebtNotificationService
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IEmailService _emailService;
  private readonly AuditService _auditService;
  private readonly ILogger<DebtNotificationService> _logger;

  public DebtNotificationService(
      IUnitOfWork unitOfWork,
      IEmailService emailService,
      AuditService auditService,
      ILogger<DebtNotificationService> logger)
  {
    _unitOfWork = unitOfWork;
    _emailService = emailService;
    _auditService = auditService;
    _logger = logger;
  }

  public async Task<int> NotifyAllDebtorsAsync()
  {
    var stats = await _auditService.GetStatsAsync();
    var debtors = stats.Debtors;
    int sentCount = 0;

    foreach (var debtor in debtors.Where(d => (d.EstimatedDebt + d.Interests) > 0))
    {
      try
      {
        var company = await _unitOfWork.Companies.GetByIdAsync(debtor.CompanyId);
        // Si la empresa no tiene email pero tiene estudio contable asociado, usamos el del estudio
        string? targetEmail = company?.ContactEmail ?? company?.AccountingStudio?.Email;

        if (string.IsNullOrEmpty(targetEmail)) continue;

        string subject = $"Aviso de Deuda Pendiente - SIRGA Sindicato";
        string body = $@"
                    <h3>Estimado/a responsable de {company?.BusinessName}:</h3>
                    <p>Le informamos que registra una deuda pendiente con nuestra institución según el sistema SIRGA.</p>
                    <ul>
                        <li><b>Última Presentación:</b> {debtor.LastPeriodPresented}</li>
                        <li><b>Deuda Estimada (Capital):</b> {debtor.EstimatedDebt:C}</li>
                        <li><b>Intereses ARCA acumulados:</b> {debtor.Interests:C}</li>
                        <li><b>Total Estimado a Regularizar:</b> {(debtor.EstimatedDebt + debtor.Interests):C}</li>
                    </ul>
                    <p>Le solicitamos regularizar su situación a la brevedad para evitar mayores recargos por intereses.</p>
                    <p>Si ya ha realizado el pago, por favor ignore este mensaje o envíe el comprobante para su conciliación manual.</p>
                    <br/>
                    <p>Saludos cordiales,<br/>Secretaría de Finanzas</p>";

        await _emailService.SendEmailAsync(targetEmail, subject, body);
        sentCount++;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error enviando notificación a deudor {CompanyId}", debtor.CompanyId);
      }
    }

    return sentCount;
  }
}
