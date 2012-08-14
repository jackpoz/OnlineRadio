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
                            radio.OnCurrentSongChanged += (oldValue, newValue) =>
                                {
                                    Console.WriteLine(newValue.Artist + " - " + newValue.Title);
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
