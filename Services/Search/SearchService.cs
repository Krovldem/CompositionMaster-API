using CompositionMaster.DTO;
using CompositionMaster.Models;
using CompositionMaster.Services.Base;
using Microsoft.EntityFrameworkCore;

namespace CompositionMaster.Services.Search
{
    public class SearchService : BaseService
    {
        public SearchService(ApplicationContext context) : base(context)
        {
        }

        public async Task<List<Nomenclature>> SearchNomenclaturesAsync(
            string? code, string? name, int? typeId, int? unitId, int page, int pageSize)
        {
            var query = _context.Nomenclatures.AsQueryable();

            if (!string.IsNullOrEmpty(code))
                query = query.Where(n => n.DSECode.Contains(code));

            if (!string.IsNullOrEmpty(name))
                query = query.Where(n => n.Name.Contains(name));

            if (typeId.HasValue)
                query = query.Where(n => n.NomenclatureType == typeId);

            if (unitId.HasValue)
                query = query.Where(n => n.UnitOfMeasurement == unitId);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<WhereUsedDto>> GetWhereUsedAsync(int nomenclatureId, bool includeObsolete)
        {
            var query = _context.SpecificationComponents
                .Where(sc => sc.Nomenclature == nomenclatureId);

            if (!includeObsolete)
            {
                var activeSpecs = _context.Specifications
                    .Where(s => s.OutputDate >= DateTime.UtcNow)
                    .Select(s => s.Identifier);

                query = query.Where(sc => activeSpecs.Contains(sc.Identifier));
            }

            var result = await query.ToListAsync();
            
            var whereUsedList = new List<WhereUsedDto>();
            
            foreach (var sc in result)
            {
                var spec = await _context.Specifications
                    .FirstOrDefaultAsync(s => s.Identifier == sc.Identifier);
                    
                var owner = spec != null 
                    ? await _context.Users.FirstOrDefaultAsync(u => u.Identifier == spec.Owner)
                    : null;
                
                var isActive = spec != null && spec.OutputDate >= DateTime.UtcNow;
                
                whereUsedList.Add(new WhereUsedDto
                {
                    SpecificationId = sc.Identifier,
                    SpecificationName = spec != null ? $"Спецификация #{spec.Identifier}" : "-",
                    LineNumber = sc.LineNumber,
                    Quantity = sc.Quantity,
                    OwnerId = spec?.Owner ?? 0,
                    OwnerName = owner?.FullName ?? "-",
                    IsActive = isActive
                });
            }

            return whereUsedList;
        }

        public async Task<List<Specification>> SearchSpecificationsAsync(
            int? ownerId, DateTime? dateFrom, DateTime? dateTo, bool? isMain, int page, int pageSize)
        {
            var query = _context.Specifications.AsQueryable();

            if (ownerId.HasValue)
                query = query.Where(s => s.Owner == ownerId);

            if (dateFrom.HasValue)
                query = query.Where(s => s.InputDate >= dateFrom);

            if (dateTo.HasValue)
                query = query.Where(s => s.InputDate <= dateTo);

            if (isMain.HasValue)
                query = query.Where(s => s.IsMain == isMain);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<OperationCard>> SearchOperationCardsAsync(
            int? specificationId, string? department, string? operation, string? equipment)
        {
            var query = _context.OperationCards.AsQueryable();

            if (specificationId.HasValue)
                query = query.Where(oc => oc.Identifier == specificationId);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(oc => oc.Department.Contains(department));

            if (!string.IsNullOrEmpty(operation))
                query = query.Where(oc => oc.Operation.Contains(operation));

            if (!string.IsNullOrEmpty(equipment))
                query = query.Where(oc => oc.Equipment.Contains(equipment));

            return await query.ToListAsync();
        }

        public async Task<List<QuickSearchResultDto>> QuickSearchAsync(string query)
        {
            var results = new List<QuickSearchResultDto>();

            if (string.IsNullOrWhiteSpace(query))
                return results;

            var searchTerm = query.ToLower();

            // Поиск по номенклатуре
            var nomenclatures = await _context.Nomenclatures
                .Where(n => n.DSECode.ToLower().Contains(searchTerm) ||
                           n.Name.ToLower().Contains(searchTerm))
                .Take(10)
                .ToListAsync();
                
            foreach (var n in nomenclatures)
            {
                results.Add(new QuickSearchResultDto
                {
                    Id = n.Identifier,
                    Type = "Номенклатура",
                    Code = n.DSECode,
                    Name = n.Name,
                    Url = $"/nomenclature/{n.Identifier}"
                });
            }

            // Поиск по спецификациям
            if (int.TryParse(searchTerm, out int specId))
            {
                var specifications = await _context.Specifications
                    .Where(s => s.Identifier == specId)
                    .Take(10)
                    .ToListAsync();
                    
                foreach (var s in specifications)
                {
                    results.Add(new QuickSearchResultDto
                    {
                        Id = s.Identifier,
                        Type = "Спецификация",
                        Code = $"#{s.Identifier}",
                        Name = $"Спецификация #{s.Identifier}",
                        Url = $"/specification/{s.Identifier}"
                    });
                }
            }

            // Поиск по пользователям
            var users = await _context.Users
                .Where(u => u.FullName.ToLower().Contains(searchTerm) ||
                           u.Login.ToLower().Contains(searchTerm))
                .Take(5)
                .ToListAsync();
                
            foreach (var u in users)
            {
                results.Add(new QuickSearchResultDto
                {
                    Id = u.Identifier,
                    Type = "Пользователь",
                    Code = u.Login,
                    Name = u.FullName,
                    Url = $"/user/{u.Identifier}"
                });
            }

            return results;
        }
    }
}