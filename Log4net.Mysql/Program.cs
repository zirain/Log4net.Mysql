using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension = "config", Watch = true)]
namespace Log4net.Mysql
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = log4net.LogManager.GetLogger("MyLogger");

            log.Info("hello world! Info");
            log.Debug("hello world! Debug");


            Console.ReadKey();
        }
    }
}
