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
            parser.Parse();
            // `parser.Results` contains the pure structured data

            var csv = parser.GenerateCSV();
            //System.IO.File.WriteAllText("Huge.csv", csv); // for writing CSV
            Console.Write(csv);

            Console.Read();
        }

        // An async function just for reference
        // Not actually used
        static async Task MainAsync(string[] args)
        {
            var parser = new ArduLogParser("example.bin");
            await parser.ParseAsync();
            // `parser.Results` contains the pure structured data

            var csv = parser.GenerateCSV();
            //System.IO.File.WriteAllText("Huge.csv", csv); // for writing CSV
            Console.Write(csv);

            Console.Read();
        }
    }
}
