using LibertyUtils;

namespace FizzBuzz.Tools
{
    class ZipFiles : BaseDownloadScript
    {
        public ZipFiles()
        {
            DebugLogDir = @"C:\RG Scripts\FizzBuzz\Debug\";
            Zip();
        }

        private void Zip()
        {
            Log.Default.Write("Starting Process");
            string[] zipLoc = { @"C:\RG Scripts\FizzBuzz\Zip1\", @"C:\RG Scripts\FizzBuzz\Zip2\" };
            foreach (string file in zipLoc)
            {
                ZipUtils.CompressLogged(@"C:\RG Scripts\FizzBuzz\Files\", file);
            }
        }
    }
}
