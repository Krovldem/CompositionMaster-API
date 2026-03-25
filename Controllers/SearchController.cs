using CompositionMaster.Models;
using CompositionMaster.DTO;
using CompositionMaster.Services.Search;
using Microsoft.AspNetCore.Mvc;

namespace CompositionMaster.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly SearchService _service;

        public SearchController(SearchService service)
        {
            _service = service;
        }

        [HttpGet("nomenclatures")]
        public async Task<ActionResult<List<Nomenclature>>> SearchNomenclatures(
            [FromQuery] string? code,
            [FromQuery] string? name,
            [FromQuery] int? typeId,
            [FromQuery] int? unitId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await _service.SearchNomenclaturesAsync(code, name, typeId, unitId, page, pageSize);
        }

        [HttpGet("where-used/{nomenclatureId}")]
        public async Task<ActionResult<List<WhereUsedDto>>> GetWhereUsed(
            int nomenclatureId,
            [FromQuery] bool includeObsolete = false)
        {
            return await _service.GetWhereUsedAsync(nomenclatureId, includeObsolete);
        }

        [HttpGet("specifications")]
        public async Task<ActionResult<List<Specification>>> SearchSpecifications(
            [FromQuery] int? ownerId,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] bool? isMain,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await _service.SearchSpecificationsAsync(ownerId, dateFrom, dateTo, isMain, page, pageSize);
        }

        [HttpGet("operation-cards")]
        public async Task<ActionResult<List<OperationCard>>> SearchOperationCards(
            [FromQuery] int? specificationId,
            [FromQuery] string? department,
            [FromQuery] string? operation,
            [FromQuery] string? equipment)
        {
            return await _service.SearchOperationCardsAsync(specificationId, department, operation, equipment);
        }

        [HttpGet("quick")]
        public async Task<ActionResult<List<QuickSearchResultDto>>> QuickSearch([FromQuery] string query)
        {
            return await _service.QuickSearchAsync(query);
        }
    }
}