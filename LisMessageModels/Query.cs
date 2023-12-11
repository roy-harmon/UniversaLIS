namespace UniversaLIS.Models
{
     public class Query
    {
        public string QueryMessage
        {
            get
            {
                return GetQueryString();
            }
            set
            {
                SetQueryString(value);
            }
        }

        public Dictionary<string, string> Elements { get; set; } = new Dictionary<string, string>();

        private string GetQueryString()
        {    // This method shouldn't actually be used, since the LIS shouldn't be sending any queries.
             // Anything missing should be added as an empty string.
            string[] elementArray = { "FrameNumber", "Sequence #", "Starting Range", "Ending Range", "Test ID", "Request Time Limits", "Beginning request results date and time", "Ending request results date and time", "Physician name", "Physician Phone Number", "User Field 1", "User Field 2", "Status Codes" };
            foreach (var item in elementArray)
            {
                if (!Elements.ContainsKey(item))
                {
                    Elements.Add(item, "");
                }
            }
            string output = Constants.STX + Elements["FrameNumber"].Trim('Q') + "Q|";
            // Concatenate the Dictionary values and return the string.
            output += Elements["Sequence #"] + "|";
            output += Elements["Starting Range"] + "|";
            output += Elements["Ending Range"] + "|";
            output += Elements["Test ID"] + "|";
            output += Elements["Request Time Limits"] + "|";
            output += Elements["Beginning request results date and time"] + "|";
            output += Elements["Ending request results date and time"] + "|";
            output += Elements["Physician name"] + "|";
            output += Elements["Physician Phone Number"] + "|";
            output += Elements["User Field 1"] + "|";
            output += Elements["User Field 2"] + "|";
            output += Elements["Status Codes"] + Constants.CR + Constants.ETX;
            return output;
        }

        private void SetQueryString(string input)
        {
            string[] inArray = input.Split('|');
            if (inArray.Length < 13)
            {
                // Invalid number of elements.
                throw new ArgumentException($"Invalid number of elements in query record string. Expected: 13 \tFound: {inArray.Length} \tString: \n{input}");
            }
            Elements["FrameNumber"] = inArray[0];
            Elements["Sequence #"] = inArray[1];
            Elements["Starting Range"] = inArray[2];
            Elements["Ending Range"] = inArray[3];
            Elements["Test ID"] = inArray[4];
            Elements["Request Time Limits"] = inArray[5];
            Elements["Beginning request results date and time"] = inArray[6];
            Elements["Ending request results date and time"] = inArray[7];
            Elements["Physician name"] = inArray[8];
            Elements["Physician Phone Number"] = inArray[9];
            Elements["User Field 1"] = inArray[10];
            Elements["User Field 2"] = inArray[11];
            Elements["Status Codes"] = inArray[12];
        }

        public Query(string queryMessage)
        {
            SetQueryString(queryMessage);
        }

        public Query()
        {    // Unused, since the LIS doesn't query the IMMULITE.
            SetQueryString("2Q|1|||ALL||||||||O");
        }
    }
}