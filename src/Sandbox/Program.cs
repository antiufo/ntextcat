using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new NTextCat.RankedLanguageIdentifierFactory(
     5,
     4000,
     0,
     int.MaxValue,
     false);
            using (var stream = File.Open(@"C:\Repositories\Awdee\Awdee2.Declarative\LanguageModels.dat", FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            {

                var l = factory.LoadBinary(stream);
                var z = l.Identify("ma come va allora, sei felice?");
                Console.WriteLine(z.First().Item1.Iso639_2T);
            }
        }
    }
}
