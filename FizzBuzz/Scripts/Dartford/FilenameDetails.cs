using LibertyUtils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FizzBuzz.Scripts.Dartford
{
    public class FilenameDetails
    {
        private static Dictionary<string, string> _batchTypeLookup = new Dictionary<string, string>()
        {
            { "MTCPCN", "CCTVPCN" },
            { "MTCCC", "CC" },
            { "MTCNODR", "NoDR" },
            { "CORRES", "CORRES" },
        };

        public string FilenameBatchType;
        public string BatchType;
        public int BatchNo;

        public FilenameDetails(string batchName)
        {
            Match m = Regex.Match(batchName, @"DFFC_IA_[0-9]{6}_([A-Z]{5,})_([0-9]+)");
            if (!m.Success)
            {
                throw new Exception("Batch name does not match pattern : " + batchName);
            }
            FilenameBatchType = m.Groups[1].Value;
            BatchType = _batchTypeLookup.LookupLogged(FilenameBatchType);
            BatchNo = Convert.ToInt32(m.Groups[2].Value);
        }
    }
}
