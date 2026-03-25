using CompositionMaster.Models;
using CompositionMaster.Services.Composition;
using Microsoft.AspNetCore.Mvc;

namespace CompositionMaster.Controllers
{
    [ApiController]
    [Route("api/specifications/{specificationId}/operation-cards")]
    public class OperationCardsController : ControllerBase
    {
        private readonly OperationCardService _service;
        private readonly SpecificationService _specificationService;

        public OperationCardsController(OperationCardService service, SpecificationService specificationService)
        {
            _service = service;
            _specificationService = specificationService;
        }

        // Получить все ОК спецификации
        [HttpGet]
        public ActionResult<List<OperationCard>> GetOperationCards(int specificationId)
        {
            if (!_specificationService.SpecificationExists(specificationId))
                return NotFound($"Спецификация {specificationId} не найдена");
                
            return _service.GetOperationCards(specificationId);
        }

        // Получить конкретную ОК
        [HttpGet("{lineNumber}")]
        public ActionResult<OperationCard> GetOperationCard(int specificationId, int lineNumber)
        {
            var card = _service.GetOperationCard(specificationId, lineNumber);
            return card is { } oc ? Ok(oc) : NotFound();
        }

        // Создать ОК
        [HttpPost]
        public IActionResult CreateOperationCard(int specificationId, OperationCard card)
        {
            if (specificationId != card.Identifier) 
                return BadRequest("ID спецификации не совпадает");
                
            if (!_specificationService.SpecificationExists(specificationId))
                return NotFound($"Спецификация {specificationId} не найдена");
                
            if (_service.OperationCardLineExists(specificationId, card.LineNumber))
                return BadRequest($"Строка {card.LineNumber} уже существует");
                
            _service.CreateOperationCard(specificationId, card, HttpContext);
            
            return CreatedAtAction(nameof(GetOperationCard), 
                new { specificationId, lineNumber = card.LineNumber }, card);
        }

        // Обновить ОК
        [HttpPut("{lineNumber}")]
        public IActionResult UpdateOperationCard(int specificationId, int lineNumber, OperationCard card)
        {
            if (specificationId != card.Identifier || lineNumber != card.LineNumber)
                return BadRequest();
                
            var oldVersion = _service.GetOperationCard(specificationId, lineNumber);
            if (oldVersion == null) return NotFound();
            
            _service.UpdateOperationCard(specificationId, lineNumber, card, HttpContext);
            
            return NoContent();
        }

        // Удалить ОК
        [HttpDelete("{lineNumber}")]
        public IActionResult DeleteOperationCard(int specificationId, int lineNumber)
        {
            var card = _service.GetOperationCard(specificationId, lineNumber);
            if (card == null) return NotFound();
            
            return _service.DeleteOperationCard(specificationId, lineNumber, HttpContext) ? NoContent() : NotFound();
        }

        // Массовое создание ОК
        [HttpPost("batch")]
        public IActionResult CreateOperationCards(int specificationId, List<OperationCard> cards)
        {
            if (!_specificationService.SpecificationExists(specificationId))
                return NotFound($"Спецификация {specificationId} не найдена");
                
            foreach (var card in cards)
            {
                if (specificationId != card.Identifier)
                    return BadRequest($"ID спецификации не совпадает для строки {card.LineNumber}");
                    
                if (_service.OperationCardLineExists(specificationId, card.LineNumber))
                    return BadRequest($"Строка {card.LineNumber} уже существует");
            }
            
            _service.CreateOperationCardsBatch(specificationId, cards, HttpContext);
            
            return Ok(cards);
        }
    }
}