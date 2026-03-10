using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using WellDataImport.Models;
using WellDataImport.Services;

namespace WellDataImport.Tests
{
    [TestFixture]
    public class WellDataServiceTests
    {
        // простой тест сводки
        [Test]
        public void BuildSummaries_OneWell_CalculatesDepthAndPorosity()
        {
            var service = new WellDataService();

            var well = new Well();
            well.WellId = "A-001";
            well.X = 100;
            well.Y = 200;
            well.Intervals.Add(new WellDataImport.Models.Interval
            {
                DepthFrom = 0,
                DepthTo = 10,
                Rock = "Sandstone",
                Porosity = 0.2
            });
            well.Intervals.Add(new WellDataImport.Models.Interval
            {
                DepthFrom = 10,
                DepthTo = 30,
                Rock = "Limestone",
                Porosity = 0.1
            });

            var wells = new List<Well>();
            wells.Add(well);

            IList<WellSummary> summaries = service.BuildSummaries(wells);

            Assert.AreEqual(1, summaries.Count);
            var summary = summaries[0];

            Assert.AreEqual("A-001", summary.WellId);
            Assert.AreEqual(30, summary.TotalDepth);
            Assert.AreEqual(2, summary.IntervalCount);
            Assert.GreaterOrEqual(summary.AveragePorosity, 0.13);
            Assert.LessOrEqual(summary.AveragePorosity, 0.14);
        }

        // тест валидации DepthFrom >= DepthTo
        [Test]
        public void LoadFromCsv_DepthFromGreaterThanDepthTo_ReturnsError()
        {
            var csv = new StringBuilder();
            csv.AppendLine("WellId;X;Y;DepthFrom;DepthTo;Rock;Porosity");
            csv.AppendLine("BAD-001;100;200;10;5;Sandstone;0.2");

            string path = Path.GetTempFileName();
            File.WriteAllText(path, csv.ToString(), Encoding.UTF8);

            var service = new WellDataService();

            try
            {
                var result = service.LoadFromCsv(path);

                Assert.AreEqual(0, result.wells.Count);
                Assert.IsTrue(result.errors.Any(e =>
                    e.WellId == "BAD-001" &&
                    e.Message.Contains("DepthFrom должен быть меньше DepthTo")));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}