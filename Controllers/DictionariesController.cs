using CompositionMaster.Models;
using CompositionMaster.Services.Dictionaries;
using Microsoft.AspNetCore.Mvc;

namespace CompositionMaster.Controllers
{
    [ApiController]
    [Route("api/dictionaries")]
    public class DictionariesController : ControllerBase
    {
        private readonly DictionaryService _service;

        public DictionariesController(DictionaryService service)
        {
            _service = service;
        }

        // ===== РОЛИ =====
        [HttpGet("roles")]
        public ActionResult<List<Role>> GetRoles() => _service.GetRoles();
        
        [HttpGet("roles/{id}")]
        public ActionResult<Role> GetRole(int id) 
            => _service.GetRole(id) is { } r ? Ok(r) : NotFound();

        [HttpPost("roles")]
        public IActionResult CreateRole(Role role)
        {
            _service.CreateRole(role);
            return CreatedAtAction(nameof(GetRole), new { id = role.Identifier }, role);
        }

        [HttpPut("roles/{id}")]
        public IActionResult UpdateRole(int id, Role role)
        {
            if (id != role.Identifier) return BadRequest();
            _service.UpdateRole(role);
            return NoContent();
        }

        [HttpDelete("roles/{id}")]
        public IActionResult DeleteRole(int id)
            => _service.DeleteRole(id) ? NoContent() : NotFound();

        // ===== ЕДИНИЦЫ ИЗМЕРЕНИЯ =====
        [HttpGet("units")]
        public ActionResult<List<UnitOfMeasurement>> GetUnits() 
            => _service.GetUnits();
        
        [HttpGet("units/{id}")]
        public ActionResult<UnitOfMeasurement> GetUnit(int id) 
            => _service.GetUnit(id) is { } u ? Ok(u) : NotFound();

        [HttpPost("units")]
        public IActionResult CreateUnit(UnitOfMeasurement unit)
        {
            _service.CreateUnit(unit);
            return CreatedAtAction(nameof(GetUnit), new { id = unit.Identifier }, unit);
        }

        [HttpPut("units/{id}")]
        public IActionResult UpdateUnit(int id, UnitOfMeasurement unit)
        {
            if (id != unit.Identifier) return BadRequest();
            _service.UpdateUnit(unit);
            return NoContent();
        }

        [HttpDelete("units/{id}")]
        public IActionResult DeleteUnit(int id)
            => _service.DeleteUnit(id) ? NoContent() : NotFound();

        // ===== ДОЛЖНОСТИ =====
        [HttpGet("positions")]
        public ActionResult<List<Position>> GetPositions() 
            => _service.GetPositions();
        
        [HttpGet("positions/{id}")]
        public ActionResult<Position> GetPosition(int id) 
            => _service.GetPosition(id) is { } p ? Ok(p) : NotFound();

        [HttpPost("positions")]
        public IActionResult CreatePosition(Position position)
        {
            _service.CreatePosition(position);
            return CreatedAtAction(nameof(GetPosition), new { id = position.Identifier }, position);
        }

        [HttpPut("positions/{id}")]
        public IActionResult UpdatePosition(int id, Position position)
        {
            if (id != position.Identifier) return BadRequest();
            _service.UpdatePosition(position);
            return NoContent();
        }

        [HttpDelete("positions/{id}")]
        public IActionResult DeletePosition(int id)
            => _service.DeletePosition(id) ? NoContent() : NotFound();

        // ===== ВИДЫ НОМЕНКЛАТУРЫ =====
        [HttpGet("nomenclature-types")]
        public ActionResult<List<NomenclatureType>> GetNomenclatureTypes() 
            => _service.GetNomenclatureTypes();
        
        [HttpGet("nomenclature-types/{id}")]
        public ActionResult<NomenclatureType> GetNomenclatureType(int id) 
            => _service.GetNomenclatureType(id) is { } nt ? Ok(nt) : NotFound();

        [HttpPost("nomenclature-types")]
        public IActionResult CreateNomenclatureType(NomenclatureType type)
        {
            _service.CreateNomenclatureType(type);
            return CreatedAtAction(nameof(GetNomenclatureType), new { id = type.Identifier }, type);
        }

        [HttpPut("nomenclature-types/{id}")]
        public IActionResult UpdateNomenclatureType(int id, NomenclatureType type)
        {
            if (id != type.Identifier) return BadRequest();
            _service.UpdateNomenclatureType(type);
            return NoContent();
        }

        [HttpDelete("nomenclature-types/{id}")]
        public IActionResult DeleteNomenclatureType(int id)
            => _service.DeleteNomenclatureType(id) ? NoContent() : NotFound();
    }
}