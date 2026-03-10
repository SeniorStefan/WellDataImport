using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WellDataImport.Models
{
    /// <summary>
    /// Сводная информация по скважине: глубина, количество интервалов, пористость и доминирующая порода.
    /// </summary>
    public class WellSummary
    {
        /// <summary>
        /// Идентификатор скважины.
        /// </summary>
        public string WellId { get; set; }
        /// <summary>
        /// Координата X.
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Координата Y.
        /// </summary>
        public double Y { get; set; }
        /// <summary>
        /// Общая глубина скважины (максимальное DepthTo по интервалам).
        /// </summary>
        public double TotalDepth { get; set; }
        /// <summary>
        /// Количество интервалов по скважине.
        /// </summary>
        public int IntervalCount { get; set; }
        /// <summary>
        /// Средняя пористость, взвешенная по толщине интервалов.
        /// </summary>
        public double AveragePorosity { get; set; }
        /// <summary>
        /// Порода с максимальной суммарной толщиной.
        /// </summary>
        public string DominantRock { get; set; }

        /// <summary>
        /// Средняя пористость в процентах для отображения в UI.
        /// </summary>
        public string AveragePorosityPercent => $"{Math.Round(AveragePorosity * 100, 2)} %";
    }
}
