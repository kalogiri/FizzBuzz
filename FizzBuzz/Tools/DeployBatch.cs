using System;
using System.IO;

namespace FizzBuzz.Tools
{
    internal class DeployBatch
    {
        public DeployBatch()
        {
            const string type = "Download";
            // Creating batch file
            const string batchFileLocation = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\CreateBatchFile\TestBatch.bat";
            string batchFileContents = 
$@"set deploy_to={(type.Contains("Download") ? @"\\ppwatch-2\Data\Warwickshire\Process\Download\" : @"\\ppwatch-2\Data\Warwickshire\Process\Upload\")} 

@echo off 
IF NOT EXIST ""%deploy_to%"" ( 
    echo ""!!!! Directory does not exist !!!!!"" 
    echo ""%deploy_to%"" 
    pause 
    exit /B
)  

for /f ""tokens=2 delims=="" %%a in ('wmic OS Ged localdatetime /value') do set ""dt=%%a"" 
set ""YY=%dt:~2,2%"" & set ""YYYY=%dt:~0,4%"" & set ""MM=%dt:~4,2%"" & set ""DD=%dt:~6,2%""
set ""HH =% dt:~8,2 % "" & set ""Min =% dt:~10,2 % "" & set ""Sec =% dt:~12,2 % ""
set ""datestamp=%YYYY%-%MM%-%DD%"" & set ""timestamp=%HH%%Min%%Sec%""
set ""fullstamp=%YYYY%%MM%%DD%_%HH%-%Min%-%Sec%"" 

set ""deploy_dir=%deploy_to%exe-%datestamp%\""

@echo on
mkdir ""%deploy_dir%""
xcopy /S/E/V/Q/F/H/I/N bin\Debug ""%deploy_dir%""
pause
%SystemRoot%\explorer.exe ""%deploy_dir%""";

            Console.WriteLine(@"Creating batch file");
            using (StreamWriter writer = new StreamWriter(batchFileLocation))
            {
                if (!File.Exists(batchFileLocation))
                {
                    File.Create(batchFileLocation);
                }
                writer.Write(batchFileContents);
            }
        }
    }
}
