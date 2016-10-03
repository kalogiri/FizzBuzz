using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FizzBuzz.Scripts
{
    class RunningScriptWithArgument
    {
        public RunningScriptWithArgument(string[] argument)
        {
            if(argument[0] == "true")
            {
                TestArgument();
            }
            else
            {
                Console.WriteLine("Invalid argument");
            }
        }

        private void TestArgument()
        {
            Console.WriteLine("true");
        }
    }
}
