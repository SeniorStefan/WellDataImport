using System.Collections.Generic;

namespace WellDataImport.Models
{
    /// <summary>
    /// Модель скважины (Well) с координатами и списком интервалов.
    /// </summary>
    public class Well
    {
        /// <summary>
        /// Идентификатор скважины, например "A-001".
        /// </summary>
        public string WellId { get; set; }

        /// <summary>
        /// Координата X скважины.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Координата Y скважины.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Интервалы по данной скважине, отсортировка выполняется уже в сервисе.
        /// </summary>
        public List<Interval> Intervals { get; } = new List<Interval>();
    }
}
