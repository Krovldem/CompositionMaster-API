using CompositionMaster.DTO;
using CompositionMaster.Models;
using CompositionMaster.Services.Base;
using Microsoft.EntityFrameworkCore;

namespace CompositionMaster.Services.History
{
    public class HistoryService : BaseService
    {
        public HistoryService(ApplicationContext context) : base(context)
        {
        }

        public async Task<List<AuditEntryDto>> GetAuditLogAsync(DateTime? from, DateTime? to, int? userId, string? entityType)
        {
            var auditEntries = new List<AuditEntryDto>();

            // Номенклатура
            var nomenclatureChanges = await _context.NomenclatureChanges
                .Where(nc => (!from.HasValue || nc.ChangeDate >= from) &&
                             (!to.HasValue || nc.ChangeDate <= to) &&
                             (!userId.HasValue || nc.Author == userId))
                .Select(nc => new AuditEntryDto
                {
                    ChangeDate = nc.ChangeDate,
                    EntityType = "Номенклатура",
                    EntityId = nc.Identifier,
                    Action = "Изменение",
                    Comment = nc.Comment,
                    Author = nc.Author,
                    AuthorName = _context.Users
                        .Where(u => u.Identifier == nc.Author)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                })
                .ToListAsync();
            auditEntries.AddRange(nomenclatureChanges);

            // Спецификации
            var specificationChanges = await _context.SpecificationChanges
                .Where(sc => (!from.HasValue || sc.ChangeDate >= from) &&
                             (!to.HasValue || sc.ChangeDate <= to) &&
                             (!userId.HasValue || sc.Author == userId))
                .Select(sc => new AuditEntryDto
                {
                    ChangeDate = sc.ChangeDate,
                    EntityType = "Спецификация",
                    EntityId = sc.Identifier,
                    Action = "Изменение",
                    Comment = sc.Comment,
                    Author = sc.Author,
                    AuthorName = _context.Users
                        .Where(u => u.Identifier == sc.Author)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                })
                .ToListAsync();
            auditEntries.AddRange(specificationChanges);

            // Компоненты спецификаций
            var componentChanges = await _context.SpecificationComponentChanges
                .Where(scc => (!from.HasValue || scc.ChangeDate >= from) &&
                              (!to.HasValue || scc.ChangeDate <= to) &&
                              (!userId.HasValue || scc.Author == userId))
                .Select(scc => new AuditEntryDto
                {
                    ChangeDate = scc.ChangeDate,
                    EntityType = "Компонент",
                    EntityId = scc.Identifier,
                    Action = "Изменение",
                    Comment = scc.Comment ?? "",
                    Author = scc.Author,
                    AuthorName = _context.Users
                        .Where(u => u.Identifier == scc.Author)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                })
                .ToListAsync();
            auditEntries.AddRange(componentChanges);

            // Операционные карты
            var operationCardChanges = await _context.OperationCardChanges
                .Where(occ => !userId.HasValue || occ.Author == userId)
                .Select(occ => new AuditEntryDto
                {
                    ChangeDate = DateTime.UtcNow,
                    EntityType = "Операционная карта",
                    EntityId = occ.Identifier,
                    Action = "Изменение",
                    Comment = $"Строка {occ.LineNumber}",
                    Author = occ.Author,
                    AuthorName = _context.Users
                        .Where(u => u.Identifier == occ.Author)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                })
                .ToListAsync();
            auditEntries.AddRange(operationCardChanges);

            // Фильтрация по типу сущности
            if (!string.IsNullOrEmpty(entityType))
            {
                auditEntries = auditEntries.Where(a => a.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return auditEntries.OrderByDescending(a => a.ChangeDate).ToList();
        }

        public async Task<AuditStatisticsDto> GetAuditStatisticsAsync(DateTime? from, DateTime? to)
        {
            var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var toDate = to ?? DateTime.UtcNow;

            var stats = new AuditStatisticsDto
            {
                PeriodFrom = fromDate,
                PeriodTo = toDate,
                TotalChanges = 0,
                ChangesByUser = new Dictionary<string, int>(),
                ChangesByEntityType = new Dictionary<string, int>()
            };

            // Номенклатура
            var nomChanges = await _context.NomenclatureChanges
                .Where(nc => nc.ChangeDate >= fromDate && nc.ChangeDate <= toDate)
                .ToListAsync();
            stats.TotalChanges += nomChanges.Count;

            foreach (var change in nomChanges)
            {
                var userName = await _context.Users
                    .Where(u => u.Identifier == change.Author)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync() ?? "Unknown";

                if (!stats.ChangesByUser.ContainsKey(userName))
                    stats.ChangesByUser[userName] = 0;
                stats.ChangesByUser[userName]++;

                if (!stats.ChangesByEntityType.ContainsKey("Номенклатура"))
                    stats.ChangesByEntityType["Номенклатура"] = 0;
                stats.ChangesByEntityType["Номенклатура"]++;
            }

            // Спецификации
            var specChanges = await _context.SpecificationChanges
                .Where(sc => sc.ChangeDate >= fromDate && sc.ChangeDate <= toDate)
                .ToListAsync();
            stats.TotalChanges += specChanges.Count;

            foreach (var change in specChanges)
            {
                var userName = await _context.Users
                    .Where(u => u.Identifier == change.Author)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync() ?? "Unknown";

                if (!stats.ChangesByUser.ContainsKey(userName))
                    stats.ChangesByUser[userName] = 0;
                stats.ChangesByUser[userName]++;

                if (!stats.ChangesByEntityType.ContainsKey("Спецификация"))
                    stats.ChangesByEntityType["Спецификация"] = 0;
                stats.ChangesByEntityType["Спецификация"]++;
            }

            // Компоненты спецификаций
            var compChanges = await _context.SpecificationComponentChanges
                .Where(scc => scc.ChangeDate >= fromDate && scc.ChangeDate <= toDate)
                .ToListAsync();
            stats.TotalChanges += compChanges.Count;

            foreach (var change in compChanges)
            {
                var userName = await _context.Users
                    .Where(u => u.Identifier == change.Author)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync() ?? "Unknown";

                if (!stats.ChangesByUser.ContainsKey(userName))
                    stats.ChangesByUser[userName] = 0;
                stats.ChangesByUser[userName]++;

                if (!stats.ChangesByEntityType.ContainsKey("Компонент"))
                    stats.ChangesByEntityType["Компонент"] = 0;
                stats.ChangesByEntityType["Компонент"]++;
            }

            // Операционные карты
            var opChanges = await _context.OperationCardChanges
                .ToListAsync();
            stats.TotalChanges += opChanges.Count;

            foreach (var change in opChanges)
            {
                var userName = await _context.Users
                    .Where(u => u.Identifier == change.Author)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync() ?? "Unknown";

                if (!stats.ChangesByUser.ContainsKey(userName))
                    stats.ChangesByUser[userName] = 0;
                stats.ChangesByUser[userName]++;

                if (!stats.ChangesByEntityType.ContainsKey("Операционная карта"))
                    stats.ChangesByEntityType["Операционная карта"] = 0;
                stats.ChangesByEntityType["Операционная карта"]++;
            }

            return stats;
        }
    }
}