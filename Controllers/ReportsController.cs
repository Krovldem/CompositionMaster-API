using CompositionMaster.Services.Composition;
using CompositionMaster.Services.Reports;
using Microsoft.AspNetCore.Mvc;

namespace CompositionMaster.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;
        private readonly SpecificationService _specificationService;

        public ReportsController(ReportService reportService, SpecificationService specificationService)
        {
            _reportService = reportService;
            _specificationService = specificationService;
        }

        [HttpGet("specification/{id}/full-structure")]
        public async Task<IActionResult> GetFullStructureReport(int id, [FromQuery] string format = "pdf")
        {
            if (!_specificationService.SpecificationExists(id))
                return NotFound($"Спецификация {id} не найдена");

            try
            {
                if (format.ToLower() == "pdf")
                {
                    var report = await _reportService.GenerateFullStructurePdfAsync(id);
                    return File(report, "application/pdf", $"specification_{id}_structure.pdf");
                }
                else if (format.ToLower() == "excel")
                {
                    var report = await _reportService.GenerateFullStructureExcelAsync(id);
                    return File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"specification_{id}_structure.xlsx");
                }
                
                return BadRequest("Поддерживаемые форматы: pdf, excel");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка генерации отчета: {ex.Message}");
            }
        }

        [HttpGet("specification/{id}/flat")]
        public async Task<IActionResult> GetFlatReport(int id, [FromQuery] string format = "excel")
        {
            if (!_specificationService.SpecificationExists(id))
                return NotFound($"Спецификация {id} не найдена");

            try
            {
                if (format.ToLower() == "pdf")
                {
                    var report = await _reportService.GenerateFlatPdfAsync(id);
                    return File(report, "application/pdf", $"specification_{id}_flat.pdf");
                }
                else if (format.ToLower() == "excel")
                {
                    var report = await _reportService.GenerateFlatExcelAsync(id);
                    return File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"specification_{id}_flat.xlsx");
                }
                
                return BadRequest("Поддерживаемые форматы: pdf, excel");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка генерации отчета: {ex.Message}");
            }
        }

        [HttpGet("specification/{id}/operation-cards")]
        public async Task<IActionResult> GetOperationCardsReport(int id, [FromQuery] string format = "pdf")
        {
            if (!_specificationService.SpecificationExists(id))
                return NotFound($"Спецификация {id} не найдена");

            try
            {
                if (format.ToLower() == "pdf")
                {
                    var report = await _reportService.GenerateOperationCardsPdfAsync(id);
                    return File(report, "application/pdf", $"specification_{id}_operation_cards.pdf");
                }
                else if (format.ToLower() == "excel")
                {
                    var report = await _reportService.GenerateOperationCardsExcelAsync(id);
                    return File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"specification_{id}_operation_cards.xlsx");
                }
                
                return BadRequest("Поддерживаемые форматы: pdf, excel");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка генерации отчета: {ex.Message}");
            }
        }

        [HttpGet("nomenclature/{id}/usage")]
        public async Task<IActionResult> GetNomenclatureUsageReport(int id, [FromQuery] string format = "pdf")
        {
            try
            {
                if (format.ToLower() == "pdf")
                {
                    var report = await _reportService.GenerateNomenclatureUsagePdfAsync(id);
                    return File(report, "application/pdf", $"nomenclature_{id}_usage.pdf");
                }
                else if (format.ToLower() == "excel")
                {
                    var report = await _reportService.GenerateNomenclatureUsageExcelAsync(id);
                    return File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"nomenclature_{id}_usage.xlsx");
                }
                
                return BadRequest("Поддерживаемые форматы: pdf, excel");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка генерации отчета: {ex.Message}");
            }
        }

        [HttpGet("changes")]
        public async Task<IActionResult> GetChangesReport(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int? authorId,
            [FromQuery] string format = "pdf")
        {
            try
            {
                if (format.ToLower() == "pdf")
                {
                    var report = await _reportService.GenerateChangesPdfAsync(from, to, authorId);
                    return File(report, "application/pdf", $"changes_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
                }
                else if (format.ToLower() == "excel")
                {
                    var report = await _reportService.GenerateChangesExcelAsync(from, to, authorId);
                    return File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"changes_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
                }
                
                return BadRequest("Поддерживаемые форматы: pdf, excel");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка генерации отчета: {ex.Message}");
            }
        }
    }
}