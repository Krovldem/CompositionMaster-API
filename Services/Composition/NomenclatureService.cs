using CompositionMaster.Models;
using CompositionMaster.DTO;
using CompositionMaster.Services.Base;
using CompositionMaster.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CompositionMaster.Services.Composition
{
    public class NomenclatureService : BaseService
    {
        private readonly UserHelper _userHelper;

        public NomenclatureService(ApplicationContext context, UserHelper userHelper) : base(context)
        {
            _userHelper = userHelper;
        }

        public List<Nomenclature> GetNomenclaturesWithDetails()
        {
            return _context.Nomenclatures
                .AsNoTracking()
                .ToList();
        }

        public Nomenclature? GetNomenclatureWithDetails(int id)
        {
            return _context.Nomenclatures
                .FirstOrDefault(n => n.Identifier == id);
        }

        public void CreateNomenclature(Nomenclature nomenclature, HttpContext httpContext)
        {
            Create(nomenclature);
            
            var change = new NomenclatureChange
            {
                Identifier = nomenclature.Identifier,
                IntroducedIntoUse = nomenclature.IntroducedIntoUse,
                DSECode = nomenclature.DSECode,
                Name = nomenclature.Name,
                SubsystemCode = nomenclature.SubsystemCode,
                ChangeDate = DateTime.UtcNow,
                Comment = "Создание номенклатуры",
                Author = _userHelper.GetCurrentUserId(httpContext)
            };
            Create(change);
        }

        public void UpdateNomenclature(int id, Nomenclature nomenclature, HttpContext httpContext)
        {
            var oldVersion = Get<Nomenclature>(id);
            if (oldVersion == null) return;
            
            Update(nomenclature);
            
            var change = new NomenclatureChange
            {
                Identifier = nomenclature.Identifier,
                IntroducedIntoUse = nomenclature.IntroducedIntoUse,
                DSECode = nomenclature.DSECode,
                Name = nomenclature.Name,
                SubsystemCode = nomenclature.SubsystemCode,
                ChangeDate = DateTime.UtcNow,
                Comment = _userHelper.GetChangesDescription(oldVersion, nomenclature),
                Author = _userHelper.GetCurrentUserId(httpContext)
            };
            Create(change);
        }

        public bool DeleteNomenclature(int id)
        {
            if (IsNomenclatureInUse(id))
                return false;
                
            return Delete<Nomenclature>(id);
        }

        public bool IsNomenclatureInUse(int nomenclatureId)
        {
            return _context.SpecificationComponents
                .Any(sc => sc.Nomenclature == nomenclatureId);
        }

        // История изменений
        public List<NomenclatureChange> GetNomenclatureHistory(int nomenclatureId)
        {
            return _context.NomenclatureChanges
                .Where(nc => nc.Identifier == nomenclatureId)
                .OrderBy(nc => nc.ChangeDate)
                .ToList();
        }

        public async Task<NomenclatureChange?> GetNomenclatureAtDateAsync(int id, DateTime date)
        {
            return await _context.NomenclatureChanges
                .Where(nc => nc.Identifier == id && nc.ChangeDate <= date)
                .OrderByDescending(nc => nc.ChangeDate)
                .FirstOrDefaultAsync();
        }

        public async Task<ComparisonResultDto> CompareNomenclatureVersionsAsync(int version1Id, int version2Id)
        {
            var version1 = await _context.NomenclatureChanges.FindAsync(version1Id);
            var version2 = await _context.NomenclatureChanges.FindAsync(version2Id);

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
    }
}