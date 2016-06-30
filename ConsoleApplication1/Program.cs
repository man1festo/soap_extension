using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            localhost.Service1 myMathService = new localhost.Service1();
            Console.Write("5 + 4 = {0}", myMathService.Add(5, 4));
            Console.Read();
        }
    }
}
