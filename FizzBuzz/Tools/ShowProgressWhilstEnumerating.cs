using System;
using CSharpTest.Net.IO;
using System.Threading;

namespace FizzBuzz.Tools
{
    class ShowProgressWhilstEnumerating
    {
        public ShowProgressWhilstEnumerating()
        {
            string path = @"\\mass-storage\MassStorage\ParkingArchive\Haringey Council\";
            Compute(path);
        }

        private void Compute(string pathToEnumerate)
        {
            long total = 0;
            FindFile fcounter = new FindFile(
                rootDirectory: pathToEnumerate, 
                filePattern: "*", 
                recursive: true, 
                includeFolders: true, 
                includeFiles: true
            );

            fcounter.RaiseOnAccessDenied = false;
            fcounter.FileFound +=
            (o, e) => 
            {
                if (!e.IsDirectory)
                {
                    Interlocked.Increment(ref total);
                }
            };

            // Start a high-priority thread to perform the accumulation
            Thread t = new Thread(fcounter.Find)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "file enum"
            };
            t.Start();

            // Allow the accumulator thread to get a head-start on us
            do { Thread.Sleep(100); }
            while (total < 100 && t.IsAlive);

            // Now we can process the files normally and update a percentage
            long count = 0, percentage = 0;
            FindFile task = new FindFile
            (
                rootDirectory: pathToEnumerate,
                filePattern: "*",
                recursive: true,
                includeFolders: true,
                includeFiles: true
            );
            task.RaiseOnAccessDenied = false;
            task.FileFound +=
                (o, e) =>
                {
                    if (!e.IsDirectory)
                    {
                        // The File that gets processed here.
                        // DoSomeProcess()
                        long progress = ++count * 100 / Interlocked.Read(ref total);
                        if(progress > percentage && progress <= 100)
                        {
                            percentage = progress;
                            Console.WriteLine("{0}% complete.", percentage);
                            //Console.WriteLine(Interlocked.Read(ref TOTAL));
                        }
                    }
                };
            task.Find();
        }
    }
}
