using CompositionMaster.Models;
using CompositionMaster.Services.Composition;
using CompositionMaster.Services.Dictionaries;
using CompositionMaster.Services.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace CompositionMaster.Controllers
{
    [ApiController]
    [Route("api/operations")]
    public class OperationsController : ControllerBase
    {
        private readonly DictionaryService _dictionaryService;
        private readonly SpecificationService _specificationService;
        private readonly UserHelper _userHelper;

        public OperationsController(
            DictionaryService dictionaryService,
            SpecificationService specificationService,
            UserHelper userHelper)
        {
            _dictionaryService = dictionaryService;
            _specificationService = specificationService;
            _userHelper = userHelper;
        }

        // ===== ROLE =====
        [HttpPost("roles")]
        public IActionResult CreateRole(Role role)
        {
            _dictionaryService.CreateRole(role);
            return CreatedAtAction(nameof(CreateRole), new { id = role.Identifier }, role);
        }

        [HttpPut("roles/{id}")]
        public IActionResult UpdateRole(int id, Role role)
        {
            if (id != role.Identifier) return BadRequest();
            _dictionaryService.UpdateRole(role);
            return NoContent();
        }

        [HttpDelete("roles/{id}")]
        public IActionResult DeleteRole(int id)
            => _dictionaryService.DeleteRole(id) ? NoContent() : NotFound();

        // ===== SPECIFICATION =====
        [HttpPost("specifications")]
        public IActionResult CreateSpecification(Specification s)
        {
            _specificationService.CreateSpecification(s, HttpContext);
            return CreatedAtAction(nameof(CreateSpecification), new { id = s.Identifier }, s);
        }

        [HttpPut("specifications/{id}")]
        public IActionResult UpdateSpecification(int id, Specification s)
        {
            if (id != s.Identifier) return BadRequest();
            _specificationService.UpdateSpecification(id, s, HttpContext);
            return NoContent();
        }

        [HttpDelete("specifications/{id}")]
        public IActionResult DeleteSpecification(int id)
            => _specificationService.DeleteSpecification(id) ? NoContent() : NotFound();

        // ===== COMPONENT (COMPOSITE KEY) =====
        [HttpPost("specification-components")]
        public IActionResult CreateComponent(SpecificationComponent sc)
        {
            _specificationService.CreateSpecificationComponent(sc, HttpContext);
            return Ok(sc);
        }

        [HttpPut("specification-components/{identifier}/{lineNumber}")]
        public IActionResult UpdateComponent(
            int identifier,
            int lineNumber,
            SpecificationComponent sc)
        {
            if (identifier != sc.Identifier || lineNumber != sc.LineNumber)
                return BadRequest();

            _specificationService.UpdateSpecificationComponent(identifier, lineNumber, sc, HttpContext);
            return NoContent();
        }

        [HttpDelete("specification-components/{identifier}/{lineNumber}")]
        public IActionResult DeleteComponent(int identifier, int lineNumber)
            => _specificationService.DeleteSpecificationComponent(identifier, lineNumber, HttpContext)
                ? NoContent()
                : NotFound();
    }
}