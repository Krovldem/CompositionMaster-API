using CompositionMaster.Models;
using CompositionMaster.Services.Base;
using Microsoft.EntityFrameworkCore;

namespace CompositionMaster.Services.Dictionaries
{
    public class DictionaryService : BaseService
    {
        public DictionaryService(ApplicationContext context) : base(context)
        {
        }

        // Роли
        public List<Role> GetRoles() => GetAll<Role>();
        public Role? GetRole(int id) => Get<Role>(id);
        public void CreateRole(Role role) => Create(role);
        public void UpdateRole(Role role) => Update(role);
        public bool DeleteRole(int id) => Delete<Role>(id);

        // Пользователи с ролями
        public List<User> GetUsersWithRoles()
        {
            return _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .ToList();
        }
        
        public User? GetUserWithRole(int id)
        {
            return _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Identifier == id);
        }
        
        public void UpdateUserRole(int userId, int roleId)
        {
            var user = Get<User>(userId);
            if (user != null)
            {
                user.RoleId = roleId;
                Update(user);
            }
        }

        // Единицы измерения
        public List<UnitOfMeasurement> GetUnits() => GetAll<UnitOfMeasurement>();
        public UnitOfMeasurement? GetUnit(int id) => Get<UnitOfMeasurement>(id);
        public void CreateUnit(UnitOfMeasurement unit) => Create(unit);
        public void UpdateUnit(UnitOfMeasurement unit) => Update(unit);
        public bool DeleteUnit(int id) 
        {
            if (_context.Nomenclatures.Any(n => n.UnitOfMeasurement == id))
                return false;
            return Delete<UnitOfMeasurement>(id);
        }

        // Должности
        public List<Position> GetPositions() => GetAll<Position>();
        public Position? GetPosition(int id) => Get<Position>(id);
        public void CreatePosition(Position position) => Create(position);
        public void UpdatePosition(Position position) => Update(position);
        public bool DeletePosition(int id) => Delete<Position>(id);

        // Виды номенклатуры
        public List<NomenclatureType> GetNomenclatureTypes() => GetAll<NomenclatureType>();
        public NomenclatureType? GetNomenclatureType(int id) => Get<NomenclatureType>(id);
        public void CreateNomenclatureType(NomenclatureType type) => Create(type);
        public void UpdateNomenclatureType(NomenclatureType type) => Update(type);
        public bool DeleteNomenclatureType(int id)
        {
            if (_context.Nomenclatures.Any(n => n.NomenclatureType == id))
                return false;
            return Delete<NomenclatureType>(id);
        }
    }
}