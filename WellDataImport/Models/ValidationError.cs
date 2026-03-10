using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WellDataImport.Models
{
    /// <summary>
    /// Описание ошибки валидации при чтении CSV.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Номер строки в исходном CSV (1-based). 0 - если строку определить нельзя.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Идентификатор скважины, для которой возникла ошибка (может быть пустым).
        /// </summary>
        public string WellId { get; set; }

        /// <summary>
        /// Текстовое описание ошибки.
        /// </summary>
        public string Message { get; set; }
    }
}
