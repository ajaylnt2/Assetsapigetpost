using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestSample
{
    public static class FileLog
    {
        public static void Log(string logText)
        {
            File.AppendAllText(@"D:\Queries2.txt", logText + "\n");
        }
    }
}
