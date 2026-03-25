using Microsoft.EntityFrameworkCore;
using CompositionMaster.Models;
using System.Security.Claims;
using System.Text;
using CompositionMaster.DTO;

namespace CompositionMaster.Services
{
    // Этот класс оставлен для обратной совместимости
    // Рекомендуется использовать новые специализированные сервисы
    public class GenericService
    {
        private readonly ApplicationContext _context;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public GenericService(ApplicationContext context, IHttpContextAccessor? httpContextAccessor = null)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public List<T> GetAll<T>() where T : class
        {
            return _context.Set<T>()
                .AsNoTracking()
                .ToList();
        }

        public T? Get<T>(params object[] keyValues) where T : class
        {
            return _context.Set<T>().Find(keyValues);
        }

        public void Create<T>(T entity) where T : class
        {
            _context.Set<T>().Add(entity);
            _context.SaveChanges();
        }

        public void Update<T>(T entity) where T : class
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public bool Delete<T>(params object[] keyValues) where T : class
        {
            var entity = _context.Set<T>().Find(keyValues);
            if (entity == null)
                return false;

            _context.Set<T>().Remove(entity);
            _context.SaveChanges();
            return true;
        }

        public void DeleteAll<T>() where T : class
        {
            var entities = _context.Set<T>().ToList();
            _context.Set<T>().RemoveRange(entities);
            _context.SaveChanges();
        }

        public List<Nomenclature> GetNomenclaturesWithDetails()
        {
            return _context.Nomenclatures
                .AsNoTracking()
                .ToList();
        }

        public List<Specification> GetSpecificationsWithOwner()
        {
            return _context.Specifications
                .AsNoTracking()
                .ToList();
        }

        public List<SpecificationComponent> GetSpecificationComponents(int specIdentifier)
        {
            return _context.SpecificationComponents
                .Where(sc => sc.Identifier == specIdentifier)
                .AsNoTracking()
                .ToList();
        }

        public bool SpecificationExists(int specIdentifier)
        {
            return _context.Specifications
                .Any(s => s.Identifier == specIdentifier);
        }

        public async Task<FullSpecificationDto?> GetFullSpecificationAsync(int specIdentifier)
        {
            try
            {
                var spec = await _context.Specifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Identifier == specIdentifier);

                if (spec == null)
                    return null;

                var components = await _context.SpecificationComponents
                    .Where(sc => sc.Identifier == specIdentifier)
                    .OrderBy(sc => sc.LineNumber)
                    .Select(sc => new ComponentDto
                    {
                        LineNumber = sc.LineNumber,
                        NomenclatureId = sc.Nomenclature,
                        Quantity = sc.Quantity,
                        ParticipatesInCalculation = sc.ParticipatesInCalculation,
                        NomenclatureName = _context.Nomenclatures
                            .Where(n => n.Identifier == sc.Nomenclature)
                            .Select(n => n.Name)
                            .FirstOrDefault(),
                        DSECode = _context.Nomenclatures
                            .Where(n => n.Identifier == sc.Nomenclature)
                            .Select(n => n.DSECode)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var operationCards = await _context.OperationCards
                    .Where(oc => oc.Identifier == specIdentifier)
                    .OrderBy(oc => oc.LineNumber)
                    .Select(oc => new OperationCardDto
                    {
                        LineNumber = oc.LineNumber,
                        Department = oc.Department,
                        Section = oc.Section,
                        Operation = oc.Operation,
                        Equipment = oc.Equipment,
                        TimeNorm = oc.TimeNorm,
                        Tariff = oc.Tariff,
                        Cost = oc.Cost,
                        Sum = oc.Sum
                    })
                    .ToListAsync();

                var changes = await _context.SpecificationChanges
                    .Where(sc => sc.Identifier == specIdentifier)
                    .OrderByDescending(sc => sc.ChangeDate)
                    .Select(sc => new SpecificationChangeDto
                    {
                        Identifier = sc.Identifier,
                        Name = sc.Name,
                        Owner = sc.Owner,
                        InputDate = sc.InputDate,
                        OutputDate = sc.OutputDate,
                        IsMain = sc.IsMain,
                        ChangeDate = sc.ChangeDate,
                        Comment = sc.Comment,
                        Author = sc.Author
                    })
                    .ToListAsync();

                return new FullSpecificationDto
                {
                    Specification = new SpecificationDto
                    {
                        Identifier = spec.Identifier,
                        InputDate = spec.InputDate,
                        OutputDate = spec.OutputDate,
                        IsMain = spec.IsMain,
                        Owner = spec.Owner
                    },
                    Components = components,
                    OperationCards = operationCards,
                    ChangeHistory = changes.Cast<object>().ToList()
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<ComponentDto>> GetFlatSpecificationAsync(int specIdentifier)
        {
            var result = await GetFullSpecificationAsync(specIdentifier);
            return result?.Components ?? new();
        }

        public async Task<SpecificationSummaryDto?> GetSpecificationSummaryAsync(int specIdentifier)
        {
            var spec = await _context.Specifications
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Identifier == specIdentifier);

            if (spec == null) return null;

            var components = await _context.SpecificationComponents
                .Where(sc => sc.Identifier == specIdentifier)
                .ToListAsync();

            var operations = await _context.OperationCards
                .Where(oc => oc.Identifier == specIdentifier)
                .ToListAsync();

            return new SpecificationSummaryDto
            {
                SpecificationIdentifier = spec.Identifier,
                CreatedDate = spec.InputDate,
                TotalComponents = components.Count,
                TotalQuantity = components.Sum(c => c.Quantity),
                InCalculationCount = components.Count(c => c.ParticipatesInCalculation),
                TotalOperations = operations.Count,
                TotalCost = operations.Sum(o => o.Sum)
            };
        }

        public int GetCurrentUserId(HttpContext? httpContext = null)
        {
            var context = httpContext ?? _httpContextAccessor?.HttpContext;
            if (context == null) return 1;
            
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;

            return 1;
        }

        public string GetChangesDescription<T>(T oldObj, T newObj)
        {
            var changes = new StringBuilder();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name == "Identifier" || prop.Name == "ChangeDate" || prop.Name == "Author")
                    continue;

                var oldValue = prop.GetValue(oldObj);
                var newValue = prop.GetValue(newObj);

                if (!Equals(oldValue, newValue))
                {
                    if (changes.Length > 0) changes.Append("; ");
                    changes.Append($"{prop.Name}: '{oldValue}' -> '{newValue}'");
                }
            }

            return changes.Length > 0 ? changes.ToString() : "Без изменений";
        }

        public bool UserHasSpecifications(int userId)
        {
            return _context.Specifications.Any(s => s.Owner == userId);
        }

        public Nomenclature? GetNomenclatureWithDetails(int id)
        {
            return _context.Nomenclatures
                .FirstOrDefault(n => n.Identifier == id);
        }

        public bool IsNomenclatureInUse(int nomenclatureId)
        {
            return _context.SpecificationComponents
                .Any(sc => sc.Nomenclature == nomenclatureId);
        }

        public bool IsUnitInUse(int unitId)
        {
            return _context.Nomenclatures.Any(n => n.UnitOfMeasurement == unitId);
        }

        public bool IsNomenclatureTypeInUse(int typeId)
        {
            return _context.Nomenclatures.Any(n => n.NomenclatureType == typeId);
        }

        public List<OperationCard> GetOperationCards(int specificationId)
        {
            return _context.OperationCards
                .Where(oc => oc.Identifier == specificationId)
                .OrderBy(oc => oc.LineNumber)
                .ToList();
        }

        public OperationCard? GetOperationCard(int specificationId, int lineNumber)
        {
            return _context.OperationCards
                .FirstOrDefault(oc => oc.Identifier == specificationId && oc.LineNumber == lineNumber);
        }

        public bool OperationCardLineExists(int specificationId, int lineNumber)
        {
            return _context.OperationCards
                .Any(oc => oc.Identifier == specificationId && oc.LineNumber == lineNumber);
        }

        public List<NomenclatureChange> GetNomenclatureHistory(int nomenclatureId)
        {
            return _context.NomenclatureChanges
                .Where(nc => nc.Identifier == nomenclatureId)
                .OrderBy(nc => nc.ChangeDate)
                .ToList();
        }

        public List<SpecificationChange> GetSpecificationHistory(int specificationId)
        {
            return _context.SpecificationChanges
                .Where(sc => sc.Identifier == specificationId)
                .OrderByDescending(sc => sc.ChangeDate)
                .ToList();
        }

        public List<SpecificationComponentChange> GetComponentHistory(int identifier, int lineNumber)
        {
            return _context.SpecificationComponentChanges
                .Where(scc => scc.Identifier == identifier && scc.LineNumber == lineNumber)
                .OrderByDescending(scc => scc.ChangeDate)
                .ToList();
        }

        public List<OperationCardChange> GetOperationCardHistory(int identifier, int lineNumber)
        {
            return _context.OperationCardChanges
                .Where(occ => occ.Identifier == identifier && occ.LineNumber == lineNumber)
                .ToList();
        }
    }
}