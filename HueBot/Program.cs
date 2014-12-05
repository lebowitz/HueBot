using Microsoft.Azure.WebJobs;

namespace HueBot
{
    public class Program
    {
        public static void Main()
      {
          var b = new HueBot();
          b.Start();
          var host = new JobHost();
          host.RunAndBlock();
        }
    }
}
