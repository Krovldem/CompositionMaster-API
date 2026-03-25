using CompositionMaster.Models;
using CompositionMaster.DTO;
using CompositionMaster.Services.Composition;
using CompositionMaster.Services.History;
using Microsoft.AspNetCore.Mvc;

namespace CompositionMaster.Controllers
{
    [ApiController]
    [Route("api/history")]
    public class HistoryController : ControllerBase
    {
        private readonly NomenclatureService _nomenclatureService;
        private readonly SpecificationService _specificationService;
        private readonly OperationCardService _operationCardService;
        private readonly HistoryService _historyService;

        public HistoryController(
            NomenclatureService nomenclatureService,
            SpecificationService specificationService,
            OperationCardService operationCardService,
            HistoryService historyService)
        {
            _nomenclatureService = nomenclatureService;
            _specificationService = specificationService;
            _operationCardService = operationCardService;
            _historyService = historyService;
        }

        // История номенклатуры
        [HttpGet("nomenclature/{id}")]
        public ActionResult<List<NomenclatureChange>> GetNomenclatureHistory(int id)
            => _nomenclatureService.GetNomenclatureHistory(id);

        // История спецификации
        [HttpGet("specification/{id}")]
        public ActionResult<List<SpecificationChange>> GetSpecificationHistory(int id)
            => _specificationService.GetSpecificationHistory(id);

        // История компонента спецификации
        [HttpGet("specification-component/{identifier}/{lineNumber}")]
        public ActionResult<List<SpecificationComponentChange>> GetComponentHistory(int identifier, int lineNumber)
            => _specificationService.GetComponentHistory(identifier, lineNumber);

        // История операционной карты
        [HttpGet("operation-card/{identifier}/{lineNumber}")]
        public ActionResult<List<OperationCardChange>> GetOperationCardHistory(int identifier, int lineNumber)
            => _operationCardService.GetOperationCardHistory(identifier, lineNumber);

        // Общий журнал изменений
        [HttpGet("audit")]
        public async Task<ActionResult<List<AuditEntryDto>>> GetAuditLog(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? userId,
            [FromQuery] string? entityType)
        {
            return await _historyService.GetAuditLogAsync(from, to, userId, entityType);
        }

        // Статистика изменений
        [HttpGet("statistics")]
        public async Task<ActionResult<AuditStatisticsDto>> GetStatistics(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            return await _historyService.GetAuditStatisticsAsync(from, to);
        }

        // Получить версию номенклатуры на дату
        [HttpGet("nomenclature/{id}/at-date")]
        public async Task<ActionResult<NomenclatureChange>> GetNomenclatureAtDate(int id, [FromQuery] DateTime date)
            => await _nomenclatureService.GetNomenclatureAtDateAsync(id, date) is { } n ? Ok(n) : NotFound();

        // Получить версию спецификации на дату
        [HttpGet("specification/{id}/at-date")]
        public async Task<ActionResult<SpecificationChange>> GetSpecificationAtDate(int id, [FromQuery] DateTime date)
            => await _specificationService.GetSpecificationAtDateAsync(id, date) is { } s ? Ok(s) : NotFound();

        // Сравнить две версии номенклатуры
        [HttpGet("nomenclature/compare")]
        public async Task<ActionResult<ComparisonResultDto>> CompareNomenclatureVersions(
            [FromQuery] int version1Id,
            [FromQuery] int version2Id)
        {
            return await _nomenclatureService.CompareNomenclatureVersionsAsync(version1Id, version2Id);
        }

        // Сравнить две версии спецификации
        [HttpGet("specification/compare")]
        public async Task<ActionResult<ComparisonResultDto>> CompareSpecificationVersions(
            [FromQuery] int version1Id,
            [FromQuery] int version2Id)
        {
            return await _specificationService.CompareSpecificationVersionsAsync(version1Id, version2Id);
        }
    }
}