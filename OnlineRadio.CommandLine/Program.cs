using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineRadio.Core;

namespace OnlineRadio.CommandLine
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() > 1)
            {
                switch (args[0])
                {
                    case "-url":
                        using (Radio radio = new Radio(args[1]))
                        {
                            radio.OnMetadataChanged += (oldValue, newValue) =>
                                {
                                    Console.WriteLine(newValue);
                                };
                            radio.Start();
                            Console.ReadLine();
                            GC.KeepAlive(radio);
                        }
                        break;
                }
            }
        }
    }
}
