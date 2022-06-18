using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //new RestSharpExample().Execute();
            await new HttpClientExample().ExecuteAsync();

            System.Console.WriteLine("Tecle algo...");
            System.Console.ReadLine();
        }
    }
}
