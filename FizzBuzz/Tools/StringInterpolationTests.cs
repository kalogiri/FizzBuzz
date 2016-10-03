using LibertyUtils;
namespace FizzBuzz.Tools
{
    internal class StringInterpolationTests : BaseDownloadScript
    {
        public StringInterpolationTests()
        {
            DebugLogDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\StringInterpolationTest\DebugLogDir\";
            const string filename = "Something.";

            Log.Write($"The filename is {filename}");
        }
          
    }
}
