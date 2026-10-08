using SIRGA.Application.Services;
using Xunit;

namespace SIRGA.Tests;

/// <summary>
/// Pruebas del planificador de la conciliación: qué decide con cada línea del archivo del banco.
/// Los importes son inventados.
/// </summary>
public class BankReconciliationPlannerTests
{
  private static BankRenditionParser.LineaRendicion Linea(decimal importe, string cuit = "20123456789",
      string periodo = "2026-09") =>
      new(1, "06102026", "0134...", 54, "0134", "S.E.C. Santiago del Estero", periodo, cuit,
          importe, new DateTime(2026, 10, 9), importe, new DateTime(2026, 10, 19), 7, 7);

  private static BankRenditionParser.Rendicion Rendicion(decimal importe, string cuit = "20123456789") =>
      new([Linea(importe, cuit)], 1, importe, importe, []);

  private static BankReconciliationPlanner.DdjjCandidata Candidata(decimal total, bool pagada = false,
      string cuit = "20123456789", string periodo = "2026-09") =>
      new(42, periodo, cuit, total, pagada);

  [Fact]
  public void Importe_igual_al_capital_es_exacta()
  {
    var plan = new BankReconciliationPlanner().Planificar(Rendicion(10000m), [Candidata(10000m)]);

    Assert.Equal(BankReconciliationPlanner.Concordancia.Exacta, Assert.Single(plan.Lineas).Estado);
    Assert.Equal(10000m, plan.TotalARegistrar);
    Assert.False(plan.RequiereRevision);
  }

  [Fact]
  public void Pagar_mas_que_el_capital_es_interes_y_el_pago_es_completo()
  {
    // La boleta se pago vencida: el importe incluye el interes resarcitorio.
    var plan = new BankReconciliationPlanner().Planificar(Rendicion(10500m), [Candidata(10000m)]);

    var linea = Assert.Single(plan.Lineas);
    Assert.Equal(BankReconciliationPlanner.Concordancia.ConInteres, linea.Estado);
    Assert.Equal(500m, linea.Diferencia);
    Assert.Equal(10500m, plan.TotalARegistrar);
    Assert.False(plan.RequiereRevision);
  }

  [Fact]
  public void Pagar_menos_que_el_capital_es_parcial_y_requiere_revision()
  {
    var plan = new BankReconciliationPlanner().Planificar(Rendicion(9500m), [Candidata(10000m)]);

    var linea = Assert.Single(plan.Lineas);
    Assert.Equal(BankReconciliationPlanner.Concordancia.Parcial, linea.Estado);
    Assert.Equal(500m, linea.Diferencia);
    Assert.Equal(0m, plan.TotalARegistrar);
    Assert.True(plan.RequiereRevision);
  }

  [Fact]
  public void Sin_ddjj_en_sirga_el_pago_no_se_pierde_queda_para_revisar()
  {
    var plan = new BankReconciliationPlanner().Planificar(Rendicion(10000m), []);

    var linea = Assert.Single(plan.Lineas);
    Assert.Equal(BankReconciliationPlanner.Concordancia.SinDdjjEnSirga, linea.Estado);
    Assert.True(plan.RequiereRevision);
    Assert.Equal(1, plan.SinDdjj);
  }

  [Fact]
  public void Una_ddjj_ya_pagada_no_se_registra_de_nuevo()
  {
    // Reimportar el mismo archivo no tiene que duplicar el pago.
    var plan = new BankReconciliationPlanner().Planificar(Rendicion(10000m), [Candidata(10000m, pagada: true)]);

    Assert.Equal(BankReconciliationPlanner.Concordancia.YaPagada, Assert.Single(plan.Lineas).Estado);
    Assert.Equal(0m, plan.TotalARegistrar);
  }

  [Fact]
  public void Tolera_los_decimales_del_float_del_legado()
  {
    // El legado guarda 1234.5599999999999: la diferencia real es de una milionesima.
    var plan = new BankReconciliationPlanner().Planificar(Rendicion(1234.5599999999999m), [Candidata(1234.56m)]);

    Assert.Equal(BankReconciliationPlanner.Concordancia.Exacta, Assert.Single(plan.Lineas).Estado);
  }

  [Fact]
  public void El_cuit_y_el_periodo_identifican_la_ddjj()
  {
    // Hay una DDJJ, pero es de OTRO periodo: no se puede usar.
    var plan = new BankReconciliationPlanner().Planificar(
        Rendicion(10000m), [Candidata(10000m, periodo: "2026-08")]);

    Assert.Equal(BankReconciliationPlanner.Concordancia.SinDdjjEnSirga, Assert.Single(plan.Lineas).Estado);
  }
}
