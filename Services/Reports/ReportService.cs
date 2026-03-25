using CompositionMaster.Models;
using CompositionMaster.DTO;
using CompositionMaster.Services.Composition;
using CompositionMaster.Services.History;
using CompositionMaster.Services.Search;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using System.Drawing;

namespace CompositionMaster.Services.Reports
{
    public class ReportService
    {
        private readonly SpecificationService _specificationService;
        private readonly NomenclatureService _nomenclatureService;
        private readonly OperationCardService _operationCardService;
        private readonly HistoryService _historyService;
        private readonly SearchService _searchService;

        public ReportService(
            SpecificationService specificationService,
            NomenclatureService nomenclatureService,
            OperationCardService operationCardService,
            HistoryService historyService,
            SearchService searchService)
        {
            _specificationService = specificationService;
            _nomenclatureService = nomenclatureService;
            _operationCardService = operationCardService;
            _historyService = historyService;
            _searchService = searchService;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            QuestPDF.Settings.License = LicenseType.Community;
            RegisterFonts();
        }

        private void RegisterFonts()
        {
            try
            {
                string fontPath = @"C:\Windows\Fonts\";
                if (File.Exists(Path.Combine(fontPath, "arial.ttf")))
                    FontManager.RegisterFont(File.OpenRead(Path.Combine(fontPath, "arial.ttf")));
                if (File.Exists(Path.Combine(fontPath, "arialbd.ttf")))
                    FontManager.RegisterFont(File.OpenRead(Path.Combine(fontPath, "arialbd.ttf")));
                if (File.Exists(Path.Combine(fontPath, "times.ttf")))
                    FontManager.RegisterFont(File.OpenRead(Path.Combine(fontPath, "times.ttf")));
                if (File.Exists(Path.Combine(fontPath, "timesbd.ttf")))
                    FontManager.RegisterFont(File.OpenRead(Path.Combine(fontPath, "timesbd.ttf")));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка регистрации шрифтов: {ex.Message}");
            }
        }

        // Вспомогательный метод для извлечения данных из объекта истории
        private static (DateTime changeDate, string? authorName, string? comment) ExtractChangeInfo(object change)
        {
            return change switch
            {
                SpecificationChangeDto scd => (scd.ChangeDate, scd.AuthorName, scd.Comment),
                SpecificationComponentChangeDto sccd => (sccd.ChangeDate, sccd.AuthorName, sccd.Comment),
                OperationCardChangeDto occd => (occd.ChangeDate, occd.AuthorName, occd.Comment),
                _ => (DateTime.MinValue, null, null)
            };
        }

        // ==================== PDF ОТЧЕТЫ ====================

        public async Task<byte[]> GenerateFullStructurePdfAsync(int specificationId)
        {
            var fullSpec = await _specificationService.GetFullSpecificationAsync(specificationId);
            if (fullSpec == null)
                throw new Exception($"Спецификация {specificationId} не найдена");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text($"Спецификация #{specificationId}")
                            .FontFamily("Arial").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        column.Item().Text($"Дата создания: {fullSpec.Specification.InputDate:dd.MM.yyyy}")
                            .FontFamily("Arial");
                        column.Item().Text($"Владелец: {fullSpec.Specification.OwnerName ?? "Не указан"}")
                            .FontFamily("Arial");
                        column.Item().Text($"Основная: {(fullSpec.Specification.IsMain ? "Да" : "Нет")}")
                            .FontFamily("Arial");
                        if (fullSpec.Specification.OutputDate < DateTime.MaxValue)
                            column.Item().Text($"Действует до: {fullSpec.Specification.OutputDate:dd.MM.yyyy}")
                                .FontFamily("Arial");
                        column.Item().PaddingVertical(10).LineHorizontal(1);
                    });

                    page.Content().Column(column =>
                    {
                        // Компоненты
                        column.Item().Text("Состав изделия:")
                            .FontFamily("Arial").FontSize(12).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(80);
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("№").FontFamily("Arial").Bold();
                                header.Cell().Text("Код ДСЕ").FontFamily("Arial").Bold();
                                header.Cell().Text("Наименование").FontFamily("Arial").Bold();
                                header.Cell().Text("Кол-во").FontFamily("Arial").Bold();
                                header.Cell().Text("Участвует").FontFamily("Arial").Bold();
                                header.Cell().ColumnSpan(5).BorderBottom(1).BorderColor(Colors.Black);
                            });

                            foreach (var component in fullSpec.Components.OrderBy(c => c.LineNumber))
                            {
                                table.Cell().Text(component.LineNumber.ToString()).FontFamily("Arial");
                                table.Cell().Text(component.DSECode ?? "-").FontFamily("Arial");
                                table.Cell().Text(component.NomenclatureName ?? "-").FontFamily("Arial");
                                table.Cell().Text(component.Quantity.ToString("0.##")).FontFamily("Arial");
                                table.Cell().Text(component.ParticipatesInCalculation ? "Да" : "Нет").FontFamily("Arial");
                            }
                        });

                        // Операционные карты
                        if (fullSpec.OperationCards != null && fullSpec.OperationCards.Any())
                        {
                            column.Item().PaddingVertical(10).Text("Операционные карты:")
                                .FontFamily("Arial").FontSize(12).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(70);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(50);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("№").FontFamily("Arial").Bold();
                                    header.Cell().Text("Подразд.").FontFamily("Arial").Bold();
                                    header.Cell().Text("Участок").FontFamily("Arial").Bold();
                                    header.Cell().Text("Операция").FontFamily("Arial").Bold();
                                    header.Cell().Text("Оборуд.").FontFamily("Arial").Bold();
                                    header.Cell().Text("Норма").FontFamily("Arial").Bold();
                                    header.Cell().Text("Тариф").FontFamily("Arial").Bold();
                                    header.Cell().Text("Сумма").FontFamily("Arial").Bold();
                                });

                                foreach (var card in fullSpec.OperationCards.OrderBy(c => c.LineNumber))
                                {
                                    table.Cell().Text(card.LineNumber.ToString()).FontFamily("Arial");
                                    table.Cell().Text(card.Department ?? "-").FontFamily("Arial");
                                    table.Cell().Text(card.Section ?? "-").FontFamily("Arial");
                                    table.Cell().Text(card.Operation ?? "-").FontFamily("Arial");
                                    table.Cell().Text(card.Equipment ?? "-").FontFamily("Arial");
                                    table.Cell().Text(card.TimeNorm.ToString("0.##")).FontFamily("Arial");
                                    table.Cell().Text(card.Tariff.ToString("0.##")).FontFamily("Arial");
                                    table.Cell().Text(card.Sum.ToString("0.##")).FontFamily("Arial");
                                }
                            });
                        }

                        // История изменений — обрабатываем List<object> через pattern matching
                        if (fullSpec.ChangeHistory != null && fullSpec.ChangeHistory.Any())
                        {
                            column.Item().PaddingVertical(10).Text("История изменений:")
                                .FontFamily("Arial").FontSize(12).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(100);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Дата").FontFamily("Arial").Bold();
                                    header.Cell().Text("Автор").FontFamily("Arial").Bold();
                                    header.Cell().Text("Комментарий").FontFamily("Arial").Bold();
                                });

                                int count = 0;
                                foreach (var change in fullSpec.ChangeHistory)
                                {
                                    if (count >= 5) break;
                                    var (changeDate, authorName, comment) = ExtractChangeInfo(change);
                                    table.Cell().Text(changeDate.ToString("dd.MM.yyyy HH:mm")).FontFamily("Arial");
                                    table.Cell().Text(authorName ?? "-").FontFamily("Arial");
                                    table.Cell().Text(comment ?? "-").FontFamily("Arial");
                                    count++;
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница ").FontFamily("Arial");
                        x.CurrentPageNumber().FontFamily("Arial");
                        x.Span(" из ").FontFamily("Arial");
                        x.TotalPages().FontFamily("Arial");
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateFlatPdfAsync(int specificationId)
        {
            var components = await _specificationService.GetFlatSpecificationAsync(specificationId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text($"Состав спецификации #{specificationId} (плоский)")
                            .FontFamily("Arial").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        column.Item().PaddingVertical(10).LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.ConstantColumn(80);
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("№").FontFamily("Arial").Bold();
                            header.Cell().Text("Код ДСЕ").FontFamily("Arial").Bold();
                            header.Cell().Text("Наименование").FontFamily("Arial").Bold();
                            header.Cell().Text("Кол-во").FontFamily("Arial").Bold();
                        });

                        foreach (var component in components.OrderBy(c => c.LineNumber))
                        {
                            table.Cell().Text(component.LineNumber.ToString()).FontFamily("Arial");
                            table.Cell().Text(component.DSECode ?? "-").FontFamily("Arial");
                            table.Cell().Text(component.NomenclatureName ?? "-").FontFamily("Arial");
                            table.Cell().Text(component.Quantity.ToString("0.##")).FontFamily("Arial");
                        }
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateOperationCardsPdfAsync(int specificationId)
        {
            // Используем GetOperationCards из OperationCardService (не Get<Specification>)
            var cards = _operationCardService.GetOperationCards(specificationId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text($"Операционные карты спецификации #{specificationId}")
                            .FontFamily("Arial").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        column.Item().PaddingVertical(10).LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.RelativeColumn();
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(50);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("№").FontFamily("Arial").Bold();
                            header.Cell().Text("Подразд.").FontFamily("Arial").Bold();
                            header.Cell().Text("Участок").FontFamily("Arial").Bold();
                            header.Cell().Text("Операция").FontFamily("Arial").Bold();
                            header.Cell().Text("Оборуд.").FontFamily("Arial").Bold();
                            header.Cell().Text("Норма").FontFamily("Arial").Bold();
                            header.Cell().Text("Тариф").FontFamily("Arial").Bold();
                            header.Cell().Text("Сумма").FontFamily("Arial").Bold();
                        });

                        foreach (var card in cards.OrderBy(c => c.LineNumber))
                        {
                            table.Cell().Text(card.LineNumber.ToString()).FontFamily("Arial");
                            table.Cell().Text(card.Department ?? "-").FontFamily("Arial");
                            table.Cell().Text(card.Section ?? "-").FontFamily("Arial");
                            table.Cell().Text(card.Operation ?? "-").FontFamily("Arial");
                            table.Cell().Text(card.Equipment ?? "-").FontFamily("Arial");
                            table.Cell().Text(card.TimeNorm.ToString("0.##")).FontFamily("Arial");
                            table.Cell().Text(card.Tariff.ToString("0.##")).FontFamily("Arial");
                            table.Cell().Text(card.Sum.ToString("0.##")).FontFamily("Arial");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница ").FontFamily("Arial");
                        x.CurrentPageNumber().FontFamily("Arial");
                        x.Span(" из ").FontFamily("Arial");
                        x.TotalPages().FontFamily("Arial");
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateNomenclatureUsagePdfAsync(int nomenclatureId)
        {
            // Используем Get<Nomenclature> из BaseService через NomenclatureService
            var nomenclature = _nomenclatureService.Get<Nomenclature>(nomenclatureId);
            if (nomenclature == null)
                throw new Exception($"Номенклатура {nomenclatureId} не найдена");

            var usage = await _searchService.GetWhereUsedAsync(nomenclatureId, true);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text($"Применяемость: {nomenclature.DSECode} - {nomenclature.Name}")
                            .FontFamily("Arial").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                        column.Item().PaddingVertical(10).LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("№").FontFamily("Arial").Bold();
                            header.Cell().Text("Спецификация").FontFamily("Arial").Bold();
                            header.Cell().Text("Кол-во").FontFamily("Arial").Bold();
                            header.Cell().Text("Владелец").FontFamily("Arial").Bold();
                            header.Cell().Text("Статус").FontFamily("Arial").Bold();
                        });

                        int rowNum = 1;
                        foreach (var item in usage)
                        {
                            table.Cell().Text(rowNum++.ToString()).FontFamily("Arial");
                            table.Cell().Text(item.SpecificationName ?? "-").FontFamily("Arial");
                            table.Cell().Text(item.Quantity.ToString("0.##")).FontFamily("Arial");
                            table.Cell().Text(item.OwnerName ?? "-").FontFamily("Arial");
                            table.Cell().Text(item.IsActive ? "Активна" : "Неактивна").FontFamily("Arial");
                        }
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateChangesPdfAsync(DateTime from, DateTime to, int? authorId)
        {
            var auditLog = await _historyService.GetAuditLogAsync(from, to, authorId, null);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text($"Журнал изменений с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}")
                            .FontFamily("Arial").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        column.Item().PaddingVertical(10).LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Дата").FontFamily("Arial").Bold();
                            header.Cell().Text("Пользователь").FontFamily("Arial").Bold();
                            header.Cell().Text("Тип").FontFamily("Arial").Bold();
                            header.Cell().Text("ID").FontFamily("Arial").Bold();
                            header.Cell().Text("Комментарий").FontFamily("Arial").Bold();
                        });

                        foreach (var entry in auditLog)
                        {
                            table.Cell().Text(entry.ChangeDate.ToString("dd.MM.yyyy HH:mm")).FontFamily("Arial");
                            table.Cell().Text(entry.AuthorName ?? "-").FontFamily("Arial");
                            table.Cell().Text(entry.EntityType).FontFamily("Arial");
                            table.Cell().Text(entry.EntityId.ToString()).FontFamily("Arial");
                            table.Cell().Text(entry.Comment).FontFamily("Arial");
                        }
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        // ==================== EXCEL ОТЧЕТЫ ====================

        public async Task<byte[]> GenerateFullStructureExcelAsync(int specificationId)
        {
            var fullSpec = await _specificationService.GetFullSpecificationAsync(specificationId);
            if (fullSpec == null)
                throw new Exception($"Спецификация {specificationId} не найдена");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"Спецификация_{specificationId}");
            worksheet.Cells.Style.Font.Name = "Arial";
            worksheet.Cells.Style.Font.Size = 10;

            worksheet.Cells["A1"].Value = $"Спецификация #{specificationId}";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 14;
            worksheet.Cells["A2"].Value = $"Дата создания: {fullSpec.Specification.InputDate:dd.MM.yyyy}";
            worksheet.Cells["A3"].Value = $"Владелец: {fullSpec.Specification.OwnerName ?? "Не указан"}";
            worksheet.Cells["A4"].Value = $"Основная: {(fullSpec.Specification.IsMain ? "Да" : "Нет")}";

            int currentRow = 6;
            worksheet.Cells[$"A{currentRow}"].Value = "Состав изделия:";
            worksheet.Cells[$"A{currentRow}"].Style.Font.Bold = true;
            worksheet.Cells[$"A{currentRow}"].Style.Font.Size = 12;
            currentRow += 2;

            worksheet.Cells[$"A{currentRow}"].Value = "№";
            worksheet.Cells[$"B{currentRow}"].Value = "Код ДСЕ";
            worksheet.Cells[$"C{currentRow}"].Value = "Наименование";
            worksheet.Cells[$"D{currentRow}"].Value = "Кол-во";
            worksheet.Cells[$"E{currentRow}"].Value = "Участвует";

            using (var range = worksheet.Cells[$"A{currentRow}:E{currentRow}"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
            currentRow++;

            foreach (var component in fullSpec.Components.OrderBy(c => c.LineNumber))
            {
                worksheet.Cells[$"A{currentRow}"].Value = component.LineNumber;
                worksheet.Cells[$"B{currentRow}"].Value = component.DSECode ?? "-";
                worksheet.Cells[$"C{currentRow}"].Value = component.NomenclatureName ?? "-";
                worksheet.Cells[$"D{currentRow}"].Value = component.Quantity;
                worksheet.Cells[$"E{currentRow}"].Value = component.ParticipatesInCalculation ? "Да" : "Нет";
                using (var range = worksheet.Cells[$"A{currentRow}:E{currentRow}"])
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                currentRow++;
            }

            if (fullSpec.OperationCards != null && fullSpec.OperationCards.Any())
            {
                currentRow += 2;
                worksheet.Cells[$"A{currentRow}"].Value = "Операционные карты:";
                worksheet.Cells[$"A{currentRow}"].Style.Font.Bold = true;
                worksheet.Cells[$"A{currentRow}"].Style.Font.Size = 12;
                currentRow += 2;

                worksheet.Cells[$"A{currentRow}"].Value = "№";
                worksheet.Cells[$"B{currentRow}"].Value = "Подразделение";
                worksheet.Cells[$"C{currentRow}"].Value = "Участок";
                worksheet.Cells[$"D{currentRow}"].Value = "Операция";
                worksheet.Cells[$"E{currentRow}"].Value = "Оборудование";
                worksheet.Cells[$"F{currentRow}"].Value = "Норма времени";
                worksheet.Cells[$"G{currentRow}"].Value = "Тариф";
                worksheet.Cells[$"H{currentRow}"].Value = "Стоимость";
                worksheet.Cells[$"I{currentRow}"].Value = "Сумма";

                using (var range = worksheet.Cells[$"A{currentRow}:I{currentRow}"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }
                currentRow++;

                foreach (var card in fullSpec.OperationCards.OrderBy(c => c.LineNumber))
                {
                    worksheet.Cells[$"A{currentRow}"].Value = card.LineNumber;
                    worksheet.Cells[$"B{currentRow}"].Value = card.Department ?? "-";
                    worksheet.Cells[$"C{currentRow}"].Value = card.Section ?? "-";
                    worksheet.Cells[$"D{currentRow}"].Value = card.Operation ?? "-";
                    worksheet.Cells[$"E{currentRow}"].Value = card.Equipment ?? "-";
                    worksheet.Cells[$"F{currentRow}"].Value = card.TimeNorm;
                    worksheet.Cells[$"G{currentRow}"].Value = card.Tariff;
                    worksheet.Cells[$"H{currentRow}"].Value = card.Cost;
                    worksheet.Cells[$"I{currentRow}"].Value = card.Sum;
                    currentRow++;
                }
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> GenerateFlatExcelAsync(int specificationId)
        {
            var components = await _specificationService.GetFlatSpecificationAsync(specificationId);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"Состав_{specificationId}");
            worksheet.Cells.Style.Font.Name = "Arial";
            worksheet.Cells.Style.Font.Size = 10;

            worksheet.Cells["A1"].Value = $"Состав спецификации #{specificationId} (плоский)";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 14;

            worksheet.Cells["A3"].Value = "№";
            worksheet.Cells["B3"].Value = "Код ДСЕ";
            worksheet.Cells["C3"].Value = "Наименование";
            worksheet.Cells["D3"].Value = "Кол-во";

            using (var range = worksheet.Cells["A3:D3"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            int row = 4;
            foreach (var component in components.OrderBy(c => c.LineNumber))
            {
                worksheet.Cells[$"A{row}"].Value = component.LineNumber;
                worksheet.Cells[$"B{row}"].Value = component.DSECode ?? "-";
                worksheet.Cells[$"C{row}"].Value = component.NomenclatureName ?? "-";
                worksheet.Cells[$"D{row}"].Value = component.Quantity;
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> GenerateOperationCardsExcelAsync(int specificationId)
        {
            var cards = _operationCardService.GetOperationCards(specificationId);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"Операционные_карты_{specificationId}");
            worksheet.Cells.Style.Font.Name = "Arial";
            worksheet.Cells.Style.Font.Size = 10;

            worksheet.Cells["A1"].Value = $"Операционные карты спецификации #{specificationId}";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 14;

            worksheet.Cells["A3"].Value = "№";
            worksheet.Cells["B3"].Value = "Подразделение";
            worksheet.Cells["C3"].Value = "Участок";
            worksheet.Cells["D3"].Value = "Операция";
            worksheet.Cells["E3"].Value = "Оборудование";
            worksheet.Cells["F3"].Value = "Норма времени";
            worksheet.Cells["G3"].Value = "Тариф";
            worksheet.Cells["H3"].Value = "Стоимость";
            worksheet.Cells["I3"].Value = "Сумма";

            using (var range = worksheet.Cells["A3:I3"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            int row = 4;
            foreach (var card in cards.OrderBy(c => c.LineNumber))
            {
                worksheet.Cells[$"A{row}"].Value = card.LineNumber;
                worksheet.Cells[$"B{row}"].Value = card.Department ?? "-";
                worksheet.Cells[$"C{row}"].Value = card.Section ?? "-";
                worksheet.Cells[$"D{row}"].Value = card.Operation ?? "-";
                worksheet.Cells[$"E{row}"].Value = card.Equipment ?? "-";
                worksheet.Cells[$"F{row}"].Value = card.TimeNorm;
                worksheet.Cells[$"G{row}"].Value = card.Tariff;
                worksheet.Cells[$"H{row}"].Value = card.Cost;
                worksheet.Cells[$"I{row}"].Value = card.Sum;
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> GenerateNomenclatureUsageExcelAsync(int nomenclatureId)
        {
            var nomenclature = _nomenclatureService.Get<Nomenclature>(nomenclatureId);
            if (nomenclature == null)
                throw new Exception($"Номенклатура {nomenclatureId} не найдена");

            var usage = await _searchService.GetWhereUsedAsync(nomenclatureId, true);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"Применяемость_{nomenclatureId}");
            worksheet.Cells.Style.Font.Name = "Arial";
            worksheet.Cells.Style.Font.Size = 10;

            worksheet.Cells["A1"].Value = $"Применяемость: {nomenclature.DSECode} - {nomenclature.Name}";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 14;

            worksheet.Cells["A3"].Value = "№";
            worksheet.Cells["B3"].Value = "Спецификация";
            worksheet.Cells["C3"].Value = "Количество";
            worksheet.Cells["D3"].Value = "Владелец";
            worksheet.Cells["E3"].Value = "Статус";

            using (var range = worksheet.Cells["A3:E3"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            int row = 4;
            int rowNum = 1;
            foreach (var item in usage)
            {
                worksheet.Cells[$"A{row}"].Value = rowNum++;
                worksheet.Cells[$"B{row}"].Value = item.SpecificationName ?? "-";
                worksheet.Cells[$"C{row}"].Value = item.Quantity;
                worksheet.Cells[$"D{row}"].Value = item.OwnerName ?? "-";
                worksheet.Cells[$"E{row}"].Value = item.IsActive ? "Активна" : "Неактивна";
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> GenerateChangesExcelAsync(DateTime from, DateTime to, int? authorId)
        {
            var auditLog = await _historyService.GetAuditLogAsync(from, to, authorId, null);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"Изменения_{from:yyyyMMdd}_{to:yyyyMMdd}");
            worksheet.Cells.Style.Font.Name = "Arial";
            worksheet.Cells.Style.Font.Size = 10;

            worksheet.Cells["A1"].Value = $"Журнал изменений с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 14;

            worksheet.Cells["A3"].Value = "Дата";
            worksheet.Cells["B3"].Value = "Пользователь";
            worksheet.Cells["C3"].Value = "Тип";
            worksheet.Cells["D3"].Value = "ID";
            worksheet.Cells["E3"].Value = "Комментарий";

            using (var range = worksheet.Cells["A3:E3"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            int row = 4;
            foreach (var entry in auditLog)
            {
                worksheet.Cells[$"A{row}"].Value = entry.ChangeDate.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[$"B{row}"].Value = entry.AuthorName ?? "-";
                worksheet.Cells[$"C{row}"].Value = entry.EntityType;
                worksheet.Cells[$"D{row}"].Value = entry.EntityId;
                worksheet.Cells[$"E{row}"].Value = entry.Comment;
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }
    }
}