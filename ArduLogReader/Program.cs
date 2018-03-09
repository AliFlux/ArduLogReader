using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduLogReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new ArduLogParser("example.bin");
            // `csv.Results` contains the pure data

            var csv = parser.GenerateCSV();
            //System.IO.File.WriteAllText("Huge.csv", csv); // for writing CSV
            Console.Write(csv);

            Console.Read();
        }
    }
}
