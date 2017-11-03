using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace csgo_twitter
{
    class Program
    {
        static void Main(string[] args)
        {
            Session session = null;
            Settings settings = new Settings();

            if (!settings.LoadSettings(Const.SETTINGS_PATH))
            {
                Console.WriteLine("Unable to load settings. Printed a new file for you.");
                if (!settings.PrintSettings(Const.SETTINGS_PATH))
                {
                    Console.WriteLine("Unable to print a new file. Hmm...");
                }
            }
            else
            {
                if ((session = new Session(settings)).Run())
                {
                    while (session.IsRunning())
                    {
                        if (Console.ReadLine() == "q")
                        {
                            session.Kill();
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Unable to run bot.");
                }
            }

            Console.WriteLine("\n\n\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
