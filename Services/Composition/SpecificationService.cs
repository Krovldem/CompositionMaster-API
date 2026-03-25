using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using CompositionMaster.Models;
using CompositionMaster.DTO;
using CompositionMaster.Services.Base;
using CompositionMaster.Services.Helpers;

namespace CompositionMaster.Services.Composition
{
    public class SpecificationService : BaseService
    {
        private readonly UserHelper _userHelper;

        public SpecificationService(ApplicationContext context, UserHelper userHelper) : base(context)
        {
            _userHelper = userHelper;
        }

        public List<Specification> GetSpecificationsWithOwner()
        {
            return _context.Specifications
                .AsNoTracking()
                .ToList();
        }

        public bool SpecificationExists(int specIdentifier)
        {
            return _context.Specifications.Any(s => s.Identifier == specIdentifier);
        }

        public List<SpecificationComponent> GetSpecificationComponents(int specIdentifier)
        {
            return _context.SpecificationComponents
                .Where(sc => sc.Identifier == specIdentifier)
                .AsNoTracking()
                .OrderBy(sc => sc.LineNumber)
                .ToList();
        }

        /// <summary>
        /// Получает полную спецификацию с вложенными уровнями (дерево)
        /// </summary>
        public async Task<FullSpecificationTreeDto?> GetFullSpecificationTreeAsync(int specIdentifier, int maxLevel = 5)
        {
            try
            {
                var spec = await _context.Specifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Identifier == specIdentifier);

                if (spec == null)
                    return null;

                // Загружаем все номенклатуры
                var allNomenclatures = await _context.Nomenclatures
                    .AsNoTracking()
                    .ToDictionaryAsync(n => n.Identifier, n => n);

                // Загружаем все спецификации
                var allSpecifications = await _context.Specifications
                    .AsNoTracking()
                    .ToListAsync();

                // Загружаем типы номенклатур
                var nomenclatureTypes = await _context.NomenclatureTypes
                    .AsNoTracking()
                    .ToDictionaryAsync(nt => nt.Identifier, nt => nt.Name);

                // Загружаем единицы измерения
                var units = await _context.UnitOfMeasurements
                    .AsNoTracking()
                    .ToDictionaryAsync(u => u.Identifier, u => u.Abbreviation);

                // Построение дерева
                var treeData = await BuildTreeAsync(
                    specIdentifier,
                    allNomenclatures,
                    allSpecifications,
                    nomenclatureTypes,
                    units,
                    maxLevel,
                    0);

                var ownerName = await _context.Users
                    .Where(u => u.Identifier == spec.Owner)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync();

                return new FullSpecificationTreeDto
                {
                    Specification = new SpecificationDto
                    {
                        Identifier = spec.Identifier,
                        InputDate = spec.InputDate,
                        OutputDate = spec.OutputDate,
                        IsMain = spec.IsMain,
                        Owner = spec.Owner,
                        OwnerName = ownerName
                    },
                    Tree = treeData,
                    Statistics = CalculateTreeStatistics(treeData)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFullSpecificationTreeAsync: {ex.Message}");
                return null;
            }
        }

        private async Task<List<TreeNodeDto>> BuildTreeAsync(
            int specIdentifier,
            Dictionary<int, Nomenclature> allNomenclatures,
            List<Specification> allSpecifications,
            Dictionary<int, string> nomenclatureTypes,
            Dictionary<int, string> units,
            int maxLevel,
            int currentLevel)
        {
            if (currentLevel > maxLevel)
                return new List<TreeNodeDto>();

            var components = await _context.SpecificationComponents
                .Where(sc => sc.Identifier == specIdentifier)
                .OrderBy(sc => sc.LineNumber)
                .AsNoTracking()
                .ToListAsync();

            var result = new List<TreeNodeDto>();

            foreach (var component in components)
            {
                if (!allNomenclatures.TryGetValue(component.Nomenclature, out var nomenclature))
                    continue;

                var node = new TreeNodeDto
                {
                    Identifier = nomenclature.Identifier,
                    Name = nomenclature.Name,
                    DSECode = nomenclature.DSECode,
                    NomenclatureType = nomenclature.NomenclatureType,
                    Quantity = component.Quantity,
                    Level = currentLevel,
                    UnitName = units.GetValueOrDefault(nomenclature.UnitOfMeasurement, "шт"),
                    TypeName = nomenclatureTypes.GetValueOrDefault(nomenclature.NomenclatureType, "Неизвестно"),
                    Expanded = true,
                    Children = new List<TreeNodeDto>()
                };

                // Проверяем, есть ли у этой номенклатуры дочерняя спецификация
                var childSpec = allSpecifications.FirstOrDefault(s => s.Owner == nomenclature.Identifier && s.IsMain);
                
                if (childSpec != null && currentLevel < maxLevel)
                {
                    node.Children = await BuildTreeAsync(
                        childSpec.Identifier,
                        allNomenclatures,
                        allSpecifications,
                        nomenclatureTypes,
                        units,
                        maxLevel,
                        currentLevel + 1);
                }

                result.Add(node);
            }

            return result;
        }

        private TreeStatisticsDto CalculateTreeStatistics(List<TreeNodeDto> nodes)
        {
            var stats = new TreeStatisticsDto
            {
                NodesByLevel = new Dictionary<int, int>(),
                TotalNodes = 0,
                TotalLevels = 0,
                MaxDepth = 0,
                AssembliesCount = 0,
                ComponentsCount = 0
            };

            void Traverse(TreeNodeDto node, int level)
            {
                stats.TotalNodes++;
                stats.MaxDepth = Math.Max(stats.MaxDepth, level);

                if (!stats.NodesByLevel.ContainsKey(level))
                    stats.NodesByLevel[level] = 0;
                stats.NodesByLevel[level]++;

                // Узел считается сборочной единицей, если у него есть дети
                if (node.Children.Any())
                    stats.AssembliesCount++;
                else
                    stats.ComponentsCount++;

                foreach (var child in node.Children)
                {
                    Traverse(child, level + 1);
                }
            }

            foreach (var node in nodes)
            {
                Traverse(node, 0);
            }

            stats.TotalLevels = stats.NodesByLevel.Count;

            return stats;
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

                // Загружаем компоненты
                var rawComponents = await _context.SpecificationComponents
                    .Where(sc => sc.Identifier == specIdentifier)
                    .OrderBy(sc => sc.LineNumber)
                    .AsNoTracking()
                    .ToListAsync();

                // Загружаем номенклатуры
                var nomenclatureIds = rawComponents.Select(sc => sc.Nomenclature).Distinct().ToList();
                var nomenclatures = await _context.Nomenclatures
                    .Where(n => nomenclatureIds.Contains(n.Identifier))
                    .AsNoTracking()
                    .ToListAsync();

                // Загружаем типы номенклатур
                var typeIds = nomenclatures.Select(n => n.NomenclatureType).Distinct().ToList();
                var nomenclatureTypes = await _context.NomenclatureTypes
                    .Where(nt => typeIds.Contains(nt.Identifier))
                    .AsNoTracking()
                    .ToDictionaryAsync(nt => nt.Identifier, nt => nt.Name);

                // Загружаем единицы измерения
                var unitIds = nomenclatures.Select(n => n.UnitOfMeasurement).Distinct().ToList();
                var units = await _context.UnitOfMeasurements
                    .Where(u => unitIds.Contains(u.Identifier))
                    .AsNoTracking()
                    .ToDictionaryAsync(u => u.Identifier, u => u.Abbreviation);

                // Проецируем в DTO
                var components = rawComponents.Select(sc =>
                {
                    var nom = nomenclatures.FirstOrDefault(n => n.Identifier == sc.Nomenclature);
                    var typeName = nom != null 
                        ? nomenclatureTypes.GetValueOrDefault(nom.NomenclatureType, "Неизвестно") 
                        : "Неизвестно";
                    var unitAbbr = nom != null 
                        ? units.GetValueOrDefault(nom.UnitOfMeasurement, "шт") 
                        : "шт";
                        
                    return new ComponentDto
                    {
                        LineNumber = sc.LineNumber,
                        NomenclatureId = sc.Nomenclature,
                        Quantity = sc.Quantity,
                        ParticipatesInCalculation = sc.ParticipatesInCalculation,
                        NomenclatureName = nom?.Name,
                        DSECode = nom?.DSECode,
                        NomenclatureTypeName = typeName,
                        UnitAbbreviation = unitAbbr
                    };
                }).ToList();

                // Операционные карты
                var rawCards = await _context.OperationCards
                    .Where(oc => oc.Identifier == specIdentifier)
                    .OrderBy(oc => oc.LineNumber)
                    .AsNoTracking()
                    .ToListAsync();

                var operationCards = rawCards.Select(oc => new OperationCardDto
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
                }).ToList();

                // Имя владельца
                var ownerName = await _context.Users
                    .Where(u => u.Identifier == spec.Owner)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync();

                return new FullSpecificationDto
                {
                    Specification = new SpecificationDto
                    {
                        Identifier = spec.Identifier,
                        InputDate = spec.InputDate,
                        OutputDate = spec.OutputDate,
                        IsMain = spec.IsMain,
                        Owner = spec.Owner,
                        OwnerName = ownerName
                    },
                    Components = components,
                    OperationCards = operationCards,
                    ChangeHistory = new List<object>()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFullSpecificationAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<ComponentDto>> GetFlatSpecificationAsync(int specIdentifier)
        {
            var result = await GetFullSpecificationAsync(specIdentifier);
            return result?.Components ?? new List<ComponentDto>();
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

        public void CreateSpecification(Specification specification, HttpContext httpContext)
        {
            Create(specification);

            var change = new SpecificationChange
            {
                Identifier = specification.Identifier,
                Name = $"Спецификация #{specification.Identifier}",
                Owner = specification.Owner,
                InputDate = specification.InputDate,
                OutputDate = specification.OutputDate,
                IsMain = specification.IsMain,
                ChangeDate = DateTime.UtcNow,
                Comment = "Создание спецификации",
                Author = _userHelper.GetCurrentUserId(httpContext)
            };
            Create(change);
        }

        public void UpdateSpecification(int id, Specification specification, HttpContext httpContext)
        {
            var oldVersion = Get<Specification>(id);
            if (oldVersion == null) return;

            Update(specification);

            var change = new SpecificationChange
            {
                Identifier = specification.Identifier,
                Name = $"Спецификация #{specification.Identifier}",
                Owner = specification.Owner,
                InputDate = specification.InputDate,
                OutputDate = specification.OutputDate,
                IsMain = specification.IsMain,
                ChangeDate = DateTime.UtcNow,
                Comment = _userHelper.GetChangesDescription(oldVersion, specification),
                Author = _userHelper.GetCurrentUserId(httpContext)
            };
            Create(change);
        }

        public bool DeleteSpecification(int id)
        {
            if (_context.SpecificationComponents.Any(sc => sc.Identifier == id))
                return false;

            return Delete<Specification>(id);
        }

        public void CreateSpecificationComponent(SpecificationComponent component, HttpContext httpContext)
        {
            Create(component);

            var existingChange = _context.SpecificationComponentChanges
                .FirstOrDefault(scc => scc.Identifier == component.Identifier 
                                       && scc.LineNumber == component.LineNumber);
    
            if (existingChange == null)
            {
                _context.SpecificationComponentChanges.Add(new SpecificationComponentChange
                {
                    Identifier = component.Identifier,
                    LineNumber = component.LineNumber,
                    Nomenclature = component.Nomenclature,
                    Quantity = component.Quantity,
                    ParticipatesInCalculation = component.ParticipatesInCalculation,
                    ChangeDate = DateTime.UtcNow,
                    Comment = "Добавление компонента",
                    Author = _userHelper.GetCurrentUserId(httpContext)
                });
                _context.SaveChanges();
            }
        }

        public bool DeleteSpecificationComponent(int identifier, int lineNumber, HttpContext httpContext)
        {
            var component = Get<SpecificationComponent>(identifier, lineNumber);
            if (component == null) return false;

            var existingChange = _context.SpecificationComponentChanges
                .FirstOrDefault(scc => scc.Identifier == identifier && scc.LineNumber == lineNumber);

            if (existingChange != null)
            {
                existingChange.Nomenclature = component.Nomenclature;
                existingChange.Quantity = component.Quantity;
                existingChange.ParticipatesInCalculation = component.ParticipatesInCalculation;
                existingChange.ChangeDate = DateTime.UtcNow;
                existingChange.Comment = "Удаление компонента";
                existingChange.Author = _userHelper.GetCurrentUserId(httpContext);
                _context.SaveChanges();
            }

            return Delete<SpecificationComponent>(identifier, lineNumber);
        }

        public void UpdateSpecificationComponent(int identifier, int lineNumber, SpecificationComponent component, HttpContext httpContext)
        {
            var existing = _context.SpecificationComponents
                .FirstOrDefault(sc => sc.Identifier == identifier && sc.LineNumber == lineNumber);

            if (existing == null)
                throw new Exception($"Component {identifier}/{lineNumber} not found");

            var oldVersion = new SpecificationComponent
            {
                Identifier = existing.Identifier,
                LineNumber = existing.LineNumber,
                Nomenclature = existing.Nomenclature,
                Quantity = existing.Quantity,
                ParticipatesInCalculation = existing.ParticipatesInCalculation
            };

            existing.Nomenclature = component.Nomenclature;
            existing.Quantity = component.Quantity;
            existing.ParticipatesInCalculation = component.ParticipatesInCalculation;

            _context.SaveChanges();

            string changes = "";
            if (oldVersion.Nomenclature != existing.Nomenclature)
            {
                var oldNom = _context.Nomenclatures.Find(oldVersion.Nomenclature)?.Name
                             ?? oldVersion.Nomenclature.ToString();
                var newNom = _context.Nomenclatures.Find(existing.Nomenclature)?.Name
                             ?? existing.Nomenclature.ToString();
                changes += $"Номенклатура: {oldNom} → {newNom}; ";
            }
            if (oldVersion.Quantity != existing.Quantity)
                changes += $"Количество: {oldVersion.Quantity} → {existing.Quantity}; ";
            if (oldVersion.ParticipatesInCalculation != existing.ParticipatesInCalculation)
                changes += $"Участвует в расчете: {(oldVersion.ParticipatesInCalculation ? "Да" : "Нет")} → {(existing.ParticipatesInCalculation ? "Да" : "Нет")}; ";

            if (string.IsNullOrEmpty(changes))
                changes = "Обновление компонента";

            var existingChange = _context.SpecificationComponentChanges
                .FirstOrDefault(scc => scc.Identifier == identifier && scc.LineNumber == lineNumber);

            if (existingChange != null)
            {
                existingChange.Nomenclature = existing.Nomenclature;
                existingChange.Quantity = existing.Quantity;
                existingChange.ParticipatesInCalculation = existing.ParticipatesInCalculation;
                existingChange.ChangeDate = DateTime.UtcNow;
                existingChange.Comment = changes;
                existingChange.Author = _userHelper.GetCurrentUserId(httpContext);
            }
            else
            {
                _context.SpecificationComponentChanges.Add(new SpecificationComponentChange
                {
                    Identifier = identifier,
                    LineNumber = lineNumber,
                    Nomenclature = existing.Nomenclature,
                    Quantity = existing.Quantity,
                    ParticipatesInCalculation = existing.ParticipatesInCalculation,
                    ChangeDate = DateTime.UtcNow,
                    Comment = changes,
                    Author = _userHelper.GetCurrentUserId(httpContext)
                });
            }

            _context.SaveChanges();
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

        public async Task<SpecificationChange?> GetSpecificationAtDateAsync(int id, DateTime date)
        {
            return await _context.SpecificationChanges
                .Where(sc => sc.Identifier == id && sc.ChangeDate <= date)
                .OrderByDescending(sc => sc.ChangeDate)
                .FirstOrDefaultAsync();
        }

        public async Task<ComparisonResultDto> CompareSpecificationVersionsAsync(int version1Id, int version2Id)
        {
            var version1 = await _context.SpecificationChanges.FindAsync(version1Id);
            var version2 = await _context.SpecificationChanges.FindAsync(version2Id);

            if (version1 == null || version2 == null)
                return new ComparisonResultDto { Error = "Версии не найдены" };

            return CompareObjects(version1, version2);
        }

        private ComparisonResultDto CompareObjects<T>(T obj1, T obj2)
        {
            var result = new ComparisonResultDto
            {
                Differences = new List<FieldDifferenceDto>()
            };

            var properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name == "Identifier" || prop.Name == "ChangeDate" || prop.Name == "Author")
                    continue;

                var value1 = prop.GetValue(obj1);
                var value2 = prop.GetValue(obj2);

                if (!Equals(value1, value2))
                {
                    result.Differences.Add(new FieldDifferenceDto
                    {
                        FieldName = prop.Name,
                        OldValue = value1?.ToString() ?? "null",
                        NewValue = value2?.ToString() ?? "null"
                    });
                }
            }

            result.HasDifferences = result.Differences.Any();
            return result;
        }

        public bool UserHasSpecifications(int userId)
        {
            return _context.Specifications.Any(s => s.Owner == userId);
        }
    }
}