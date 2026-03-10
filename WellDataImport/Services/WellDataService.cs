using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WellDataImport.Models;
using System.IO;

using WellDataImport.Services;

namespace WellDataImport.Services
{
    public class WellDataService
    {
        /// <summary>
        /// Асинхронная обёртка над LoadFromCsv для использования вместе с async/await в UI.
        /// </summary>
        public async Task<(IList<Well> wells, IList<ValidationError> errors)> LoadFromCsvAsync(string path)
        {
            return await Task.Run(() => LoadFromCsv(path));
        }

        /// <summary>
        /// Синхронная загрузка и валидация CSV-файла.
        /// Возвращает список скважин с интервалами и список ошибок.
        /// </summary>
        public (IList<Well> wells, IList<ValidationError> errors) LoadFromCsv(string path)
        {
            var wells = new Dictionary<string, Well>();
            var errors = new List<ValidationError>();

            if (!File.Exists(path))
            {
                errors.Add(new ValidationError
                {
                    LineNumber = 0,
                    WellId = string.Empty,
                    Message = "Файл не найден."
                });
                return (wells.Values.ToList(), errors);
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError
                {
                    LineNumber = 0,
                    WellId = string.Empty,
                    Message = "Ошибка чтения файла: " + ex.Message
                });
                return (wells.Values.ToList(), errors);
            }

            if (lines.Length <= 1)
            {
                return (wells.Values.ToList(), errors);
            }

            // предполагаем, что первая строка - заголовок, поэтому начинаем с i = 1
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // разбиваем строку по разделителю ';'
                var parts = line.Split(';');
                if (parts.Length < 7)
                {
                    errors.Add(new ValidationError
                    {
                        LineNumber = i + 1,
                        WellId = string.Empty,
                        Message = "Неверное количество столбцов."
                    });
                    continue;
                }

                string wellId = parts[0].Trim();
                string xStr = parts[1].Trim();
                string yStr = parts[2].Trim();
                string depthFromStr = parts[3].Trim();
                string depthToStr = parts[4].Trim();
                string rock = parts[5].Trim();
                string porosityStr = parts[6].Trim();

                double x, y, depthFrom, depthTo, porosity;

                // парсим все числовые значения в инвариантной культуре (точка как разделитель)
                if (!double.TryParse(xStr, NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                    !double.TryParse(yStr, NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                    !double.TryParse(depthFromStr, NumberStyles.Float, CultureInfo.InvariantCulture, out depthFrom) ||
                    !double.TryParse(depthToStr, NumberStyles.Float, CultureInfo.InvariantCulture, out depthTo) ||
                    !double.TryParse(porosityStr, NumberStyles.Float, CultureInfo.InvariantCulture, out porosity))
                {
                    errors.Add(new ValidationError
                    {
                        LineNumber = i + 1,
                        WellId = wellId,
                        Message = "Не удалось распарсить числовые значения."
                    });
                    continue;
                }

                // формируем интервал
                var interval = new Interval
                {
                    DepthFrom = depthFrom,
                    DepthTo = depthTo,
                    Rock = rock,
                    Porosity = porosity
                };

                // проверяем интервал на бизнес-ошибки (глубины, пористость, порода)
                var validationMessages = ValidateInterval(interval);
                foreach (var msg in validationMessages)
                {
                    errors.Add(new ValidationError
                    {
                        LineNumber = i + 1,
                        WellId = wellId,
                        Message = msg
                    });
                }

                if (validationMessages.Any())
                {
                    continue;
                }

                Well well;
                if (!wells.TryGetValue(wellId, out well))
                {
                    well = new Well
                    {
                        WellId = wellId,
                        X = x,
                        Y = y
                    };
                    wells.Add(wellId, well);
                }

                well.Intervals.Add(interval);
            }

            // после загрузки всех данных проверяем пересечения интервалов внутри каждой скважины
            foreach (var well in wells.Values)
            {
                var sorted = well.Intervals.OrderBy(t => t.DepthFrom).ToList();
                for (int idx = 1; idx < sorted.Count; idx++)
                {
                    var prev = sorted[idx - 1];
                    var current = sorted[idx];
                    if (current.DepthFrom < prev.DepthTo)
                    {
                        errors.Add(new ValidationError
                        {
                            LineNumber = 0,
                            WellId = well.WellId,
                            Message = string.Format(CultureInfo.InvariantCulture,
                                "Интервалы пересекаются: [{0} - {1}] и [{2} - {3}].",
                                prev.DepthFrom, prev.DepthTo, current.DepthFrom, current.DepthTo)
                        });
                    }
                }
            }

            return (wells.Values.ToList(), errors);
        }

        /// <summary>
        /// Базовая валидация одного интервала по правилам задания.
        /// </summary>
        private static IList<string> ValidateInterval(Interval interval)
        {
            var result = new List<string>();

            if (interval.DepthFrom < 0)
            {
                result.Add("DepthFrom должен быть >= 0.");
            }

            if (interval.DepthFrom >= interval.DepthTo)
            {
                result.Add("DepthFrom должен быть меньше DepthTo.");
            }

            if (interval.Porosity < 0 || interval.Porosity > 1)
            {
                result.Add("Porosity должна быть в диапазоне [0..1].");
            }

            if (string.IsNullOrWhiteSpace(interval.Rock))
            {
                result.Add("Rock не должен быть пустым.");
            }

            return result;
        }

        /// <summary>
        /// Построение сводной информации по каждой скважине:
        /// общая глубина, количество интервалов, средняя пористость и доминирующая порода.
        /// </summary>
        public IList<WellSummary> BuildSummaries(IList<Well> wells)
        {
            var summaries = new List<WellSummary>();

            foreach (var well in wells)
            {
                if (well.Intervals.Count == 0)
                {
                    continue;
                }

                var totalDepth = well.Intervals.Max(i => i.DepthTo);
                var intervalCount = well.Intervals.Count;
                var totalThickness = well.Intervals.Sum(i => i.Thickness);

                double weightedPorosity = 0;
                if (totalThickness > 0)
                {
                    weightedPorosity = well.Intervals.Sum(i => i.Porosity * i.Thickness) / totalThickness;
                }

                var dominantRock = well.Intervals
                    .GroupBy(i => i.Rock)
                    .Select(g => new
                    {
                        Rock = g.Key,
                        Thickness = g.Sum(i => i.Thickness)
                    })
                    .OrderByDescending(g => g.Thickness)
                    .FirstOrDefault();

                summaries.Add(new WellSummary
                {
                    WellId = well.WellId,
                    X = well.X,
                    Y = well.Y,
                    TotalDepth = totalDepth,
                    IntervalCount = intervalCount,
                    AveragePorosity = weightedPorosity,
                    DominantRock = dominantRock != null ? dominantRock.Rock : string.Empty
                });
            }

            return summaries;
        }
    }
}
