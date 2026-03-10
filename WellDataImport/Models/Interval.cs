using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WellDataImport.Models
{
    /// <summary>
    /// Интервал по скважине: глубины, порода и пористость.
    /// </summary>
    public class Interval
    {
        /// <summary>
        /// Начало интервала по глубине (м).
        /// </summary>
        public double DepthFrom { get; set; }

        /// <summary>
        /// Конец интервала по глубине (м).
        /// </summary>
        public double DepthTo { get; set; }

        /// <summary>
        /// Порода (тип горной породы).
        /// </summary>
        public string Rock { get; set; }

        /// <summary>
        /// Пористость в диапазоне [0..1].
        /// </summary>
        public double Porosity { get; set; }

        /// <summary>
        /// Толщина интервала (DepthTo - DepthFrom). Неотрицательная.
        /// </summary>
        public double Thickness => Math.Max(0, DepthTo - DepthFrom);
    }
}
