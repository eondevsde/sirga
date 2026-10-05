using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;
using SIRGA.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace SIRGA.Application.Services;

public class EmployeeService(IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Ids de los empleados con asignación activa a un sindicato. Es lo que acota lo que puede ver un
    /// auditor. Devuelve sólo ids a propósito: la pantalla filtra lo que ya tiene cargado, así no se
    /// duplica el mapeo del DTO (que está escrito a mano y con muchos campos) ni se traen 27.934
    /// empleados de más.
    /// </summary>
    public async Task<IReadOnlyCollection<int>> GetEmployeeIdsByUnionAsync(int unionId)
    {
        return await unitOfWork.Assignments.RunQueryAsync(q => q
            .Where(a => a.UnionId == unionId && a.IsActive)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync());
    }
    public async Task<IEnumerable<EmployeeDto>> GetEmployeesByAssignmentAsync(int companyId, int unionId)
    {
        var assignments = await unitOfWork.Assignments.FindAsync(a =>
            a.CompanyId == companyId && a.UnionId == unionId && a.IsActive);

        var result = new List<EmployeeDto>();
        foreach (var assign in assignments)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(assign.EmployeeId);
            if (employee != null)
            {
                result.Add(new EmployeeDto
                {
                    Id = employee.Id,
                    Dni = employee.Dni,
                    FullName = employee.FullName,
                    JoinedDate = employee.HireDate,
                    LeftDate = assign.TerminationDate,
                    IsActive = assign.IsActive,
                    IsVoluntaryMember = assign.IsVoluntaryMember,
                    AssignmentId = assign.Id, // Agregamos el Id del Assignment para la DDJJ
                    MembershipResignationRequested = assign.MembershipResignationRequested == true
                });
            }
        }
        return result;
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesByCompanyAsync(int companyId)
    {
        var assignments = await unitOfWork.Assignments.FindAsync(a => a.CompanyId == companyId);

        var result = new List<EmployeeDto>();
        foreach (var assign in assignments)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(assign.EmployeeId);
            if (employee != null)
            {
                result.Add(new EmployeeDto
                {
                    Id = employee.Id,
                    Dni = employee.Dni,
                    FullName = employee.FullName,
                    JoinedDate = employee.HireDate,
                    LeftDate = assign.TerminationDate,
                    IsActive = assign.IsActive,
                    IsVoluntaryMember = assign.IsVoluntaryMember,
                    MembershipResignationRequested = assign.MembershipResignationRequested == true,
                    // Sin esto el Id de la asignación quedaba en 0 y el botón "Solicitar renuncia
                    // como socio" se cortaba en silencio: el diálogo nunca se abría.
                    AssignmentId = assign.Id
                });
            }
        }
        return result;
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesRawAsync()
    {
        return await unitOfWork.Employees.GetAllAsync();
    }

    /// <summary>
    /// Empleados para el padrón exportable, con su empresa cargada y el filtro resuelto en SQL.
    /// Antes se filtraba en memoria sobre <c>Assignments</c>, que nunca se cargaba (no hay lazy
    /// loading ni Includes): el filtro por empresa o ciudad devolvía una lista vacía y la columna
    /// Empresa del Excel salía siempre "N/A".
    /// </summary>
    public async Task<List<Employee>> GetEmployeesForPadronAsync(int? companyId = null, string? city = null)
    {
        var query = unitOfWork.Employees.Query()
            .AsNoTracking()
            .Include(e => e.Assignments)
                .ThenInclude(a => a.Company)
            .AsQueryable();

        if (companyId.HasValue)
        {
            var empresaId = companyId.Value;
            query = query.Where(e => e.Assignments.Any(a => a.CompanyId == empresaId));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var localidad = city.Trim();
            query = query.Where(e => e.Assignments.Any(a =>
                a.Company.City != null && a.Company.City.Name.Contains(localidad)));
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<EmployeeDto>> GetPadronAsync()
    {
        // Una sola consulta proyectada en SQL. Antes traía los 27.934 empleados y las 36.011
        // asignaciones completas —y rastreadas por el change tracker del circuito, que las
        // retenía en memoria— para cruzarlas después en C#.
        return await unitOfWork.Employees.RunQueryAsync(q =>
            q.Select(e => new EmployeeDto
            {
                Id = e.Id,
                Dni = e.Dni,
                FullName = e.FullName,
                JoinedDate = e.HireDate,
                IsActive = e.Assignments.Any(a => a.IsActive)
            }).ToListAsync());
    }

    public async Task<Company?> GetCompanyByIdAsync(int id)
    {
        return await unitOfWork.Companies.GetByIdAsync(id);
    }

    public async Task CreateEmployeeWithAssignmentAsync(CreateEmployeeDto dto)
    {
        var branch = await unitOfWork.Companies.GetByIdAsync(dto.CompanyId)
            ?? throw new Exception("La sucursal especificada no existe.");

        // LÓGICA DE NEGOCIO PARA SUCURSALES:
        // 1. Intentamos buscar un Sindicato que tenga jurisdicción específica en la CIUDAD de la SUCURSAL.
        var cityId = branch.CityId;
        if (cityId == null) throw new Exception("La empresa no tiene una localidad asignada.");

        var cityObj = await unitOfWork.Cities.GetByIdAsync(cityId.Value);
        var provinceObj = cityObj != null ? await unitOfWork.Provinces.GetByIdAsync(cityObj.ProvinceId) : null;

        var provinceName = provinceObj?.Name?.Trim() ?? "";
        var cityName = cityObj?.Name?.Trim() ?? "";

        var union = (await unitOfWork.Unions.FindAsync(u =>
            u.JurisdictionProvince.ToLower() == provinceName.ToLower() &&
            u.JurisdictionCity != null &&
            u.JurisdictionCity.ToLower() == cityName.ToLower())).FirstOrDefault();

        // 2. Si no existe un sindicato local/filial para esa ciudad, 
        // buscamos el Sindicato Provincial General (donde JurisdictionCity es null o vacío).
        if (union == null)
        {
            union = (await unitOfWork.Unions.FindAsync(u =>
                u.JurisdictionProvince.ToLower() == provinceName.ToLower() &&
                (string.IsNullOrEmpty(u.JurisdictionCity)))).FirstOrDefault();
        }

        // 3. Si aún no encontramos nada, hay un error de configuración de jurisdicciones.
        if (union == null)
        {
            throw new Exception($"No existe un Sindicato configurado para la provincia {provinceName} que cubra la ciudad de {cityName}.");
        }
        var existing = (await unitOfWork.Employees.FindAsync(e => e.Dni == dto.Dni)).FirstOrDefault();
        int employeeId;

        if (existing == null)
        {
            var newEmployee = new Employee
            {
                Dni = dto.Dni,
                FullName = dto.FullName,
                HireDate = DateTime.Now,
                Status = EmployeeStatus.Active
            };
            await unitOfWork.Employees.AddAsync(newEmployee);
            await unitOfWork.CompleteAsync();
            employeeId = newEmployee.Id;
        }
        else
        {
            employeeId = existing.Id;
        }

        // El empleado queda vinculado a la SUCURSAL especÃ­fica (CompanyId) 
        // pero bajo el SINDICATO que tiene jurisdicciÃ³n en esa zona.
        var assignment = new SindicataryAssignment
        {
            EmployeeId = employeeId,
            CompanyId = dto.CompanyId,
            UnionId = union.Id,
            IsVoluntaryMember = dto.IsVoluntaryMember
        };

        await unitOfWork.Assignments.AddAsync(assignment);
        await unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<Employee>> GetAllAsync() => await unitOfWork.Employees.GetAllAsync();

    /// <summary>
    /// Empleados paginados y ordenados por nombre, para el endpoint que antes devolvía
    /// los 27.934 registros completos en una sola respuesta.
    /// </summary>
    public async Task<IEnumerable<Employee>> GetPagedAsync(int page, int pageSize)
    {
        return await unitOfWork.Employees.RunQueryAsync(q =>
            q.OrderBy(e => e.FullName)
             .Skip((page - 1) * pageSize)
             .Take(pageSize)
             .ToListAsync());
    }

    public async Task<Employee?> GetByDniAsync(string dni)
    {
        var result = await unitOfWork.Employees.FindAsync(e => e.Dni == dni);
        return result.FirstOrDefault();
    }

    public async Task<(bool Success, string? Error)> RequestMembershipResignationAsync(int assignmentId, string reason)
    {
        var assignment = await unitOfWork.Assignments.GetByIdAsync(assignmentId);
        if (assignment == null) return (false, "La asignación no existe.");
        if (!assignment.IsActive) return (false, "El empleado no tiene una vinculación activa con la empresa.");
        if (!assignment.IsVoluntaryMember) return (false, "El empleado no es socio voluntario.");
        if (assignment.MembershipResignationRequested == true)
            return (false, "Ya existe una solicitud de renuncia pendiente para este empleado.");

        assignment.MembershipResignationRequested = true;
        assignment.MembershipResignationReason = string.IsNullOrWhiteSpace(reason) ? "Sin motivo informado" : reason;
        assignment.MembershipResignationRequestedAt = DateTime.Now;

        await unitOfWork.Assignments.UpdateAsync(assignment);
        var ok = await unitOfWork.CompleteAsync() > 0;
        return (ok, ok ? null : "No se pudo registrar la solicitud de renuncia.");
    }

    public async Task<IEnumerable<MembershipResignationDto>> GetMembershipResignationRequestsAsync(int unionId)
    {
        var assignments = await unitOfWork.Assignments.FindAsync(a =>
            a.UnionId == unionId && a.MembershipResignationRequested == true && a.IsActive);

        var result = new List<MembershipResignationDto>();
        foreach (var assign in assignments)
        {
            var employee = await unitOfWork.Employees.GetByIdAsync(assign.EmployeeId);
            var company = await unitOfWork.Companies.GetByIdAsync(assign.CompanyId);
            result.Add(new MembershipResignationDto
            {
                AssignmentId = assign.Id,
                EmployeeId = assign.EmployeeId,
                EmployeeName = employee?.FullName ?? "N/A",
                Dni = employee?.Dni ?? "N/A",
                CompanyId = assign.CompanyId,
                CompanyBusinessName = company?.BusinessName ?? "N/A",
                CompanyCuit = company?.Cuit ?? "N/A",
                UnionId = assign.UnionId,
                Reason = assign.MembershipResignationReason ?? "",
                RequestedAt = assign.MembershipResignationRequestedAt ?? DateTime.MinValue
            });
        }
        return result.OrderByDescending(x => x.RequestedAt);
    }

    public async Task<(bool Success, string? Error)> ResolveMembershipResignationAsync(int assignmentId, bool approve, string comment)
    {
        var assignment = await unitOfWork.Assignments.GetByIdAsync(assignmentId);
        if (assignment == null) return (false, "La asignación no existe.");
        if (assignment.MembershipResignationRequested != true)
            return (false, "No hay una solicitud de renuncia pendiente para esta asignación.");

        if (approve)
        {
            assignment.IsVoluntaryMember = false;
            assignment.MembershipEndDate = DateTime.Now;
        }
        assignment.MembershipResignationRequested = false;

        await unitOfWork.Assignments.UpdateAsync(assignment);
        var ok = await unitOfWork.CompleteAsync() > 0;
        return (ok, ok ? null : "No se pudo procesar la solicitud.");
    }

    public async Task AddAsync(Employee employee)
    {
        var existing = await GetByDniAsync(employee.Dni);
        if (existing != null) throw new Exception("El DNI ya se encuentra registrado.");

        await unitOfWork.Employees.AddAsync(employee);
        await unitOfWork.CompleteAsync();
    }

    /// <summary>
    /// Transfiere un empleado de una sucursal a otra.
    /// También debe verificar/actualizar el sindicato si el empleado se mueve a otra jurisdicción.
    /// </summary>
    public async Task TransferEmployeeAsync(int employeeId, int currentCompanyId, int targetCompanyId)
    {
        var assignments = await unitOfWork.Assignments.FindAsync(a =>
            a.EmployeeId == employeeId && a.CompanyId == currentCompanyId);

        var assignment = assignments.FirstOrDefault()
            ?? throw new Exception("No se encontró el vínculo del empleado con la sucursal de origen.");

        var targetBranch = await unitOfWork.Companies.GetByIdAsync(targetCompanyId)
            ?? throw new Exception("La sucursal de destino no existe.");

        // Verificar si la jurisdicción cambia
        var cityId = targetBranch.CityId;
        if (cityId == null) throw new Exception("La sucursal de destino no tiene una localidad asignada.");

        var cityObj = await unitOfWork.Cities.GetByIdAsync(cityId.Value);
        var provinceObj = cityObj != null ? await unitOfWork.Provinces.GetByIdAsync(cityObj.ProvinceId) : null;
        var provinceName = provinceObj?.Name?.Trim() ?? "";
        var cityName = cityObj?.Name?.Trim() ?? "";

        // Buscar sindicato con jurisdicción en la nueva zona
        var union = (await unitOfWork.Unions.FindAsync(u =>
            u.JurisdictionProvince.ToLower() == provinceName.ToLower() &&
            u.JurisdictionCity != null &&
            u.JurisdictionCity.ToLower() == cityName.ToLower())).FirstOrDefault();

        if (union == null)
        {
            union = (await unitOfWork.Unions.FindAsync(u =>
                u.JurisdictionProvince.ToLower() == provinceName.ToLower() &&
                (string.IsNullOrEmpty(u.JurisdictionCity)))).FirstOrDefault();
        }

        if (union == null)
        {
            throw new Exception($"No existe un Sindicato configurado para la zona {provinceName}/{cityName} de la nueva sucursal.");
        }

        // Actualizar asignación
        assignment.CompanyId = targetCompanyId;
        assignment.UnionId = union.Id;

        await unitOfWork.CompleteAsync();
    }
}

