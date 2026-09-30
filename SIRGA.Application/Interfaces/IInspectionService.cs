using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIRGA.Application.DTOs;

namespace SIRGA.Application.Interfaces;

public interface IInspectionService
{
  // Métodos existentes (solo por referencia, se asumen implementados en la clase concreta)
  // Task<IEnumerable<InspectionAct>> GetAllActsAsync();
  // Task<byte[]> GenerateActPdfAsync(int actId);
  // Task CreatePaymentPlanAsync(int actId, int installments, decimal downPayment, decimal interestRate);

  // Nuevos métodos para Inspectores
  Task<IEnumerable<InspectorDto>> GetInspectorsAsync();
  Task CreateInspectorAsync(InspectorDto inspector);
  Task UpdateInspectorAsync(InspectorDto inspector);
  Task DeleteInspectorAsync(int id);

  // Gestión de Visitas
  Task RegisterVisitAsync(VisitRegistrationDto visit);
  Task<IEnumerable<VisitDto>> GetVisitsByActIdAsync(int actId);
  Task<byte[]> GenerateVisitPdfAsync(int visitId);

  // Aviso Prejudicial
  Task CreatePrejudicialNoticeAsync(int actId, string observations);
  Task<IEnumerable<PrejudicialNoticeDto>> GetPrejudicialNoticesAsync();
  Task UploadNoticeAttachmentAsync(int noticeId, string fileName, string fileType, byte[] fileData);
  Task PromoteToJudicialAsync(int noticeId);

  // Reportes
  Task<IEnumerable<InspectorStatsDto>> GetInspectorStatsAsync(int year, int month);
}

