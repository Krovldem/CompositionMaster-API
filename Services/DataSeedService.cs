using Microsoft.EntityFrameworkCore;
using CompositionMaster.Models;
using System.Text.Json;
using System.Reflection;

namespace CompositionMaster.Services
{
    public class DataSeedService
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<DataSeedService> _logger;
        private readonly string _seedDataPath;

        public DataSeedService(
            ApplicationContext context,
            ILogger<DataSeedService> logger)
        {
            _context = context;
            _logger = logger;
            _seedDataPath = Path.Combine(Directory.GetCurrentDirectory(), "SeedData");
        }

        /// <summary>
        /// Создание или обновление схемы базы данных
        /// </summary>
        public async Task EnsureDatabaseCreatedAsync()
        {
            try
            {
                _logger.LogInformation("Проверка и создание базы данных...");
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("База данных и таблицы созданы/проверены");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании базы данных");
                throw;
            }
        }

        public async Task SeedDataAsync()
        {
            await EnsureDatabaseCreatedAsync();

            // Загружаем данные
            await SeedEntityIfChangedAsync<Role>("roles.json", r => ((Role)r).Identifier);
            await SeedEntityIfChangedAsync<Position>("positions.json", p => ((Position)p).Identifier);
            await SeedEntityIfChangedAsync<User>("users.json", u => ((User)u).Identifier);
            await SeedEntityIfChangedAsync<UnitOfMeasurement>("unitsofmeasurement.json", uom => ((UnitOfMeasurement)uom).Identifier);
            await SeedEntityIfChangedAsync<NomenclatureType>("nomenclaturetypes.json", nt => ((NomenclatureType)nt).Identifier);
            await SeedEntityIfChangedAsync<Nomenclature>("nomenclatures.json", n => ((Nomenclature)n).Identifier);
            await SeedEntityIfChangedAsync<Specification>("specifications.json", s => ((Specification)s).Identifier);
            await SeedEntityIfChangedAsync<SpecificationComponent>("specificationcomponents.json",
                sc => new { ((SpecificationComponent)sc).Identifier, ((SpecificationComponent)sc).LineNumber });
        }

        private async Task SeedEntityIfChangedAsync<T>(
            string fileName,
            Func<object, object> keySelector) where T : class
        {
            var path = Path.Combine(_seedDataPath, fileName);
            if (!File.Exists(path))
            {
                _logger.LogWarning($"Файл {fileName} не найден в {_seedDataPath}");
                return;
            }

            var json = await File.ReadAllTextAsync(path);
            var newEntities = JsonSerializer.Deserialize<List<T>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (newEntities == null || newEntities.Count == 0)
            {
                _logger.LogWarning($"Файл {fileName} пуст или не содержит данных");
                return;
            }

            NormalizeDateTimes(newEntities);

            var dbSet = _context.Set<T>();
            List<T> existingEntities;
            
            try
            {
                existingEntities = await dbSet.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Ошибка при чтении данных {typeof(T).Name}: {ex.Message}");
                return;
            }

            var addedCount = 0;
            var updatedCount = 0;
            var unchangedCount = 0;

            var existingDict = existingEntities.ToDictionary(
                e => keySelector(e),
                e => e
            );

            foreach (var newEntity in newEntities)
            {
                var key = keySelector(newEntity);

                if (existingDict.TryGetValue(key, out var existingEntity))
                {
                    if (HasChanges(existingEntity, newEntity))
                    {
                        _context.Entry(existingEntity).CurrentValues.SetValues(newEntity);
                        updatedCount++;
                        _logger.LogDebug($"Обновлена запись {typeof(T).Name} с ключом {key}");
                    }
                    else
                    {
                        unchangedCount++;
                    }
                }
                else
                {
                    await dbSet.AddAsync(newEntity);
                    addedCount++;
                    _logger.LogDebug($"Добавлена новая запись {typeof(T).Name} с ключом {key}");
                }
            }

            var deletedCount = 0;
            foreach (var existingEntity in existingEntities)
            {
                var key = keySelector(existingEntity);
                if (!newEntities.Any(e => Equals(keySelector(e), key)))
                {
                    dbSet.Remove(existingEntity);
                    deletedCount++;
                    _logger.LogDebug($"Удалена запись {typeof(T).Name} с ключом {key}");
                }
            }

            if (addedCount > 0 || updatedCount > 0 || deletedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    $"Обновление {typeof(T).Name}: добавлено {addedCount}, обновлено {updatedCount}, удалено {deletedCount}, без изменений {unchangedCount}");
            }
            else
            {
                _logger.LogInformation(
                    $"Нет изменений в {typeof(T).Name}: {unchangedCount} записей актуальны");
            }
        }

        private bool HasChanges<T>(T existing, T updated) where T : class
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.Name == "Identifier" ||
                    prop.Name == "LineNumber" ||
                    prop.Name == "Role" ||
                    prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute>() != null)
                    continue;

                var existingValue = prop.GetValue(existing);
                var newValue = prop.GetValue(updated);

                if (!Equals(existingValue, newValue))
                {
                    if (existingValue is DateTime existingDt && newValue is DateTime newDt)
                    {
                        if (existingDt.Kind != newDt.Kind)
                        {
                            var normalizedExisting = DateTime.SpecifyKind(existingDt, newDt.Kind);
                            if (normalizedExisting != newDt)
                                return true;
                        }
                        else if (existingDt != newDt)
                        {
                            return true;
                        }
                    }
                    else if (!Equals(existingValue, newValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<bool> HasAnyChangesAsync()
        {
            await EnsureDatabaseCreatedAsync();

            var hasChanges = false;

            hasChanges |= await HasEntityChangesAsync<Role>("roles.json", r => ((Role)r).Identifier);
            hasChanges |= await HasEntityChangesAsync<Position>("positions.json", p => ((Position)p).Identifier);
            hasChanges |= await HasEntityChangesAsync<User>("users.json", u => ((User)u).Identifier);
            hasChanges |= await HasEntityChangesAsync<UnitOfMeasurement>("unitsofmeasurement.json", uom => ((UnitOfMeasurement)uom).Identifier);
            hasChanges |= await HasEntityChangesAsync<NomenclatureType>("nomenclaturetypes.json", nt => ((NomenclatureType)nt).Identifier);
            hasChanges |= await HasEntityChangesAsync<Nomenclature>("nomenclatures.json", n => ((Nomenclature)n).Identifier);
            hasChanges |= await HasEntityChangesAsync<Specification>("specifications.json", s => ((Specification)s).Identifier);
            hasChanges |= await HasEntityChangesAsync<SpecificationComponent>("specificationcomponents.json",
                sc => new { ((SpecificationComponent)sc).Identifier, ((SpecificationComponent)sc).LineNumber });

            return hasChanges;
        }

        private async Task<bool> HasEntityChangesAsync<T>(
            string fileName,
            Func<object, object> keySelector) where T : class
        {
            var path = Path.Combine(_seedDataPath, fileName);
            if (!File.Exists(path)) return false;

            var json = await File.ReadAllTextAsync(path);
            var newEntities = JsonSerializer.Deserialize<List<T>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (newEntities == null || newEntities.Count == 0) return false;

            NormalizeDateTimes(newEntities);

            List<T> existingEntities;
            try
            {
                existingEntities = await _context.Set<T>().ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Ошибка при чтении данных {typeof(T).Name}: {ex.Message}");
                return false;
            }
            
            var existingDict = existingEntities.ToDictionary(e => keySelector(e), e => e);

            foreach (var newEntity in newEntities)
            {
                var key = keySelector(newEntity);

                if (existingDict.TryGetValue(key, out var existingEntity))
                {
                    if (HasChanges(existingEntity, newEntity))
                        return true;
                }
                else
                {
                    return true;
                }
            }

            foreach (var existingEntity in existingEntities)
            {
                var key = keySelector(existingEntity);
                if (!newEntities.Any(e => Equals(keySelector(e), key)))
                    return true;
            }

            return false;
        }

        private static void NormalizeDateTimes(IEnumerable<object> entities)
        {
            foreach (var entity in entities)
            {
                foreach (var p in entity.GetType().GetProperties(
                    BindingFlags.Public | BindingFlags.Instance))
                {
                    if (p.PropertyType == typeof(DateTime) &&
                        p.GetValue(entity) is DateTime dt &&
                        dt.Kind == DateTimeKind.Unspecified)
                    {
                        p.SetValue(entity, DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                    }
                }
            }
        }
    }
}