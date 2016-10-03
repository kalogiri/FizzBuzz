namespace FizzBuzz.Tools
{
    class ChangePipeToComma
    {
        public ChangePipeToComma()
        {
            Change("|", ",");
        }

        private static void Change(string orig, string replace)
        {
            string text = System.IO.File.ReadAllText(@"C:\PPProject\c# Projects\Test\uploads\ppwatch\Apcoa\Test Files\NTK & NTO Car Park_31052016_1219.txt");
            text = text.Replace(orig, replace);
            System.IO.File.WriteAllText(@"C:\PPProject\c# Projects\Test\uploads\ppwatch\Apcoa\Test Files\NTK & NTO Car Park_31052016_1219_Replaced.txt", text);
        }
    }
}
