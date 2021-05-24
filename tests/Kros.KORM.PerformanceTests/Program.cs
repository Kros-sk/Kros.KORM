using BenchmarkDotNet.Running;

namespace Kros.KORM.PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MaterializeToClassVsRecordTest>();
        }
    }
}
