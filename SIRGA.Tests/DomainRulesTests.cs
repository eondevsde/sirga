using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.Tests;

public class DomainRulesTests
{
  [Fact]
  public void InspectionAct_TotalAmount_ShouldBe_EstimatedDebt_Plus_Interest()
  {
    var act = new InspectionAct { EstimatedDebt = 1000m, InterestAmount = 250m };
    Assert.Equal(1250m, act.TotalAmount);
  }

  [Theory]
  [InlineData(0, 0, 0)]
  [InlineData(5000, 0, 5000)]
  [InlineData(0, 500, 500)]
  [InlineData(1500.50, 300.25, 1800.75)]
  public void InspectionAct_TotalAmount_WithVariousValues_ShouldSumCorrectly(
      decimal estimatedDebt, decimal interest, decimal expected)
  {
    Assert.Equal(expected, new InspectionAct { EstimatedDebt = estimatedDebt, InterestAmount = interest }.TotalAmount);
  }

  [Fact]
  public void InspectionAct_DefaultStatus_ShouldBe_Pending()
  {
    Assert.Equal(InspectionActStatus.Pending, new InspectionAct().Status);
  }

  [Fact]
  public void PaymentPlan_DefaultStatus_ShouldBe_Active()
  {
    Assert.Equal(PaymentPlanStatus.Active, new PaymentPlan().Status);
  }

  [Fact]
  public void PaymentPlan_ShouldHave_EmptyInstallments_ByDefault()
  {
    var plan = new PaymentPlan();
    Assert.NotNull(plan.Installments);
    Assert.Empty(plan.Installments);
  }

  [Fact]
  public void PaymentPlan_ShouldAddInstallments_WhenCreated()
  {
    var plan = new PaymentPlan { PlanNumber = "PP-001", TotalDebt = 10000m, DownPayment = 2000m, InstallmentsCount = 6, InstallmentAmount = 1500m };
    plan.Installments.Add(new PaymentPlanInstallment { InstallmentNumber = 0, DueDate = DateTime.Now, Amount = plan.DownPayment });
    for (int i = 1; i <= plan.InstallmentsCount; i++)
      plan.Installments.Add(new PaymentPlanInstallment { InstallmentNumber = i, DueDate = DateTime.Now.AddMonths(i), Amount = plan.InstallmentAmount });
    Assert.Equal(7, plan.Installments.Count);
  }

  [Fact]
  public void PaymentPlanInstallment_ShouldNotBePaid_ByDefault()
  {
    var i = new PaymentPlanInstallment();
    Assert.False(i.IsPaid); Assert.Null(i.PaymentDate); Assert.Null(i.PaymentReference);
  }

  [Fact]
  public void PaymentPlanInstallment_WhenPaid_ShouldSetPaymentDate()
  {
    var i = new PaymentPlanInstallment { InstallmentNumber = 1, DueDate = DateTime.Now.AddMonths(1), Amount = 1500m };
    i.IsPaid = true; i.PaymentDate = DateTime.Now; i.PaymentReference = "MP-123456";
    Assert.True(i.IsPaid); Assert.NotNull(i.PaymentDate); Assert.Equal("MP-123456", i.PaymentReference);
  }

  [Fact]
  public void Company_DefaultStatus_ShouldBe_PendingAuthorization()
  {
    Assert.Equal(CompanyStatus.PendingAuthorization, new Company().Status);
  }

  [Fact]
  public void Company_RequiredProperties_ShouldBeSet()
  {
    var c = new Company { BusinessName = "Empresa S.A.", Cuit = "30-12345678-9", Address = "Av. Siempre Viva 123" };
    Assert.Equal("Empresa S.A.", c.BusinessName); Assert.Equal("30-12345678-9", c.Cuit);
  }

  [Fact]
  public void Company_ShouldHave_EmptyCollections_ByDefault()
  {
    var c = new Company();
    Assert.Empty(c.Assignments); Assert.Empty(c.Branches); Assert.Empty(c.Declarations);
  }

  [Fact]
  public void DeclarationDetail_ShouldStore_Contributions()
  {
    var d = new DeclarationDetail { BaseSalary = 50000m, LawContribution = 1000m, MemberContribution = 500m, TotalWithheld = 1500m };
    Assert.Equal(50000m, d.BaseSalary); Assert.Equal(1000m, d.LawContribution); Assert.Equal(500m, d.MemberContribution);
  }

  [Fact]
  public void DeclarationRegistry_ShouldHave_EmptyDetails_ByDefault()
  {
    Assert.Empty(new DeclarationRegistry().Details);
  }

  [Fact]
  public void Employee_RequiredProperties_ShouldBeSet()
  {
    var e = new Employee { Dni = "20123456", FullName = "Perez, Juan", HireDate = new DateTime(2025, 1, 1), Status = EmployeeStatus.Active };
    Assert.Equal("20123456", e.Dni); Assert.Equal("Perez, Juan", e.FullName); Assert.Equal(EmployeeStatus.Active, e.Status);
  }

  [Fact]
  public void Employee_ShouldHave_EmptyAssignments_ByDefault()
  {
    Assert.Empty(new Employee().Assignments);
  }

  [Fact]
  public void InterestRate_ShouldStore_RateAndDescription()
  {
    var r = new InterestRate { EffectiveDate = new DateTime(2026, 1, 1), MonthlyRate = 3.5m, Description = "Tasa Resarcitoria" };
    Assert.Equal(3.5m, r.MonthlyRate); Assert.Equal("Tasa Resarcitoria", r.Description);
  }

  [Fact]
  public void SalaryScale_ShouldStore_Amount()
  {
    var s = new SalaryScale { Period = new DateTime(2026, 1, 1), BasicAmount = 250000m, IsActive = true };
    Assert.True(s.IsActive); Assert.Equal(250000m, s.BasicAmount);
  }

  [Fact]
  public void InspectionActStatus_ShouldHave_AllExpectedValues()
  {
    var v = Enum.GetValues<InspectionActStatus>();
    Assert.Contains(InspectionActStatus.Pending, v); Assert.Contains(InspectionActStatus.Notified, v);
    Assert.Contains(InspectionActStatus.InCovenant, v); Assert.Contains(InspectionActStatus.Paid, v);
    Assert.Contains(InspectionActStatus.Cancelled, v); Assert.Contains(InspectionActStatus.Legal, v);
  }

  [Fact]
  public void PaymentPlanStatus_ShouldHave_AllExpectedValues()
  {
    var v = Enum.GetValues<PaymentPlanStatus>();
    Assert.Contains(PaymentPlanStatus.Active, v); Assert.Contains(PaymentPlanStatus.Completed, v);
    Assert.Contains(PaymentPlanStatus.Defaulted, v); Assert.Contains(PaymentPlanStatus.Cancelled, v);
  }

  [Fact]
  public void CompanyStatus_ShouldHave_AllExpectedValues()
  {
    var v = Enum.GetValues<CompanyStatus>();
    Assert.Contains(CompanyStatus.Active, v); Assert.Contains(CompanyStatus.Inactive, v);
    Assert.Contains(CompanyStatus.Suspended, v); Assert.Contains(CompanyStatus.PendingAuthorization, v);
  }

  [Fact]
  public void DeclarationStatus_ShouldHave_AllExpectedValues()
  {
    var v = Enum.GetValues<DeclarationStatus>();
    Assert.Contains(DeclarationStatus.Draft, v); Assert.Contains(DeclarationStatus.Submitted, v);
    Assert.Contains(DeclarationStatus.Approved, v); Assert.Contains(DeclarationStatus.Rejected, v);
    Assert.Contains(DeclarationStatus.Paid, v);
  }

  [Fact]
  public void EmployeeStatus_ShouldHave_AllExpectedValues()
  {
    var v = Enum.GetValues<EmployeeStatus>();
    Assert.Contains(EmployeeStatus.Active, v); Assert.Contains(EmployeeStatus.LeaveOfAbsence, v);
    Assert.Contains(EmployeeStatus.MaternityLeave, v); Assert.Contains(EmployeeStatus.AccidentLeave, v);
    Assert.Contains(EmployeeStatus.Inactive, v);
  }
}
