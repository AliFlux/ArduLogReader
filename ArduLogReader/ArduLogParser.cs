using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class ArduLogParser
{

    // Initializing some binary format constants
    const int BLOCK_SIZE = 8192;
    const int MSG_HEADER_LEN = 3;
    const int MSG_HEAD1 = 0xA3;
    const int MSG_HEAD2 = 0x95;
    const int MSG_FORMAT_PACKET_LEN = 89;
    const string MSG_FORMAT_STRUCT = "BB4s16s64s";
    const int MSG_TYPE_FORMAT = 0x80;

    // Dictionary that maps binary format types to python format types
    Dictionary<string, Tuple<string, double>> formatStructureMapping = new Dictionary<string, Tuple<string, double>>()
    {
        { "b", new Tuple<string, double>("b", 1) },
        { "B", new Tuple<string, double>("B", 1) },
        { "h", new Tuple<string, double>("h", 1) },
        { "H", new Tuple<string, double>("H", 1) },
        { "i", new Tuple<string, double>("i", 1) },
        { "I", new Tuple<string, double>("I", 1) },
        { "f", new Tuple<string, double>("f", 1) },
        { "d", new Tuple<string, double>("d", 1) },
        { "n", new Tuple<string, double>("4s", 1) },
        { "N", new Tuple<string, double>("16s", 1) },
        { "Z", new Tuple<string, double>("64s", 1) },
        { "c", new Tuple<string, double>("h", 0.01) },
        { "C", new Tuple<string, double>("H", 0.01) },
        { "e", new Tuple<string, double>("i", 0.01) },
        { "E", new Tuple<string, double>("I", 0.01) },
        { "L", new Tuple<string, double>("i", 0.0000001) },
        { "M", new Tuple<string, double>("b", 1) },
        { "q", new Tuple<string, double>("q", 1) },
        { "Q", new Tuple<string, double>("Q", 1) },
    };

    struct MessageInfo
    {
        public int Length;
        public string Name;
        public string Format;
        public string[] Labels;
        public string Structure;
        public double[] Multipliers;
    }

    // Shared helper variables
    string filePath = "";
    int pointer = 0;
    Byte[] buffer;
    Dictionary<byte, MessageInfo> messageInfos = new Dictionary<byte, MessageInfo>();
    Dictionary<string, string[]> messageLabels = new Dictionary<string, string[]>();
    List<string> messageNames = new List<string>();
    List<string> messageColumns = new List<string>();
    Dictionary<string, string> extractions = new Dictionary<string, string>();
    List<Dictionary<string, string>> result;

    public List<Dictionary<string, string>> Result
    {
        get
        {
            return result;
        }
    }

    // Initialize and parse the file
    public ArduLogParser(string filePath)
    {
        this.filePath = filePath;
        parse();
    }

    private void parse()
    {
        var file = System.IO.File.Open(filePath, System.IO.FileMode.Open);
        int position = 0;
        Byte[] chunk = new byte[BLOCK_SIZE];
        buffer = new byte[0];
        var bytesRead = 0;
        bool isFirstRow = true;

        result = new List<Dictionary<string, string>>();
        
        while ((bytesRead = file.Read(chunk, 0, BLOCK_SIZE)) > 0)
        {
            if (chunk.Length == 0)
            {
                return;
            }

            buffer = buffer.Skip(pointer).Concat(chunk.Take(bytesRead)).ToArray();
            pointer = 0;


            while (bytesLeft() >= MSG_HEADER_LEN)
            {
                var head1 = buffer[pointer];
                var head2 = buffer[pointer + 1];

                if (head1 != MSG_HEAD1 || head2 != MSG_HEAD2)
                {
                    throw new InvalidOperationException("The file format is invalid or corrupted.");
                }

                var messageType = buffer[pointer + 2];

                if (messageType == MSG_TYPE_FORMAT)
                {
                    if (bytesLeft() < MSG_FORMAT_PACKET_LEN)
                    {
                        // Probably corrupted
                        // Writing was probably interrupted
                        break;
                    }

                    parseMessageDescription();
                }
                else
                {
                    var messageInfo = messageInfos[messageType];

                    if (bytesLeft() < messageInfo.Length)
                    {
                        break;
                    }

                    if (isFirstRow)
                    {
                        emitHeader(messageInfo);
                        isFirstRow = false;
                    }

                    parseMessage(messageInfo);
                }
            }

            bytesRead += pointer;
            position += chunk.Length;

            emitRow();
        }
    }

    void parseMessageDescription()
    {
        var unpackedData = buffer.Skip(pointer + 3).Take(MSG_FORMAT_PACKET_LEN - 3).ToArray();
        var data = unpack(MSG_FORMAT_STRUCT, unpackedData);
        var messageType = (byte)data[0];
        var messageLength = (byte)data[1];
        var messageName = (string)data[2];
        var messageFormat = (string)data[3];
        var messageLabels = data[4].ToString().Split(',');

        if (messageType != MSG_TYPE_FORMAT)
        {
            var messageStructure = "<";
            var messageMultipliers = new double[messageLabels.Length];

            int i = 0;
            foreach (var character in messageFormat)
            {
                messageStructure += formatStructureMapping[character.ToString()].Item1;
                messageMultipliers[i] = formatStructureMapping[character.ToString()].Item2;
                i++;
            }

            var info = new MessageInfo()
            {
                Length = messageLength,
                Name = messageName,
                Format = messageFormat,
                Labels = messageLabels,
                Structure = messageStructure,
                Multipliers = messageMultipliers,
            };

            messageInfos[messageType] = info;
            this.messageLabels[messageName] = messageLabels;
            messageNames.Add(messageName);
        }

        pointer += MSG_FORMAT_PACKET_LEN;
    }

    void emitHeader(MessageInfo messageInfo)
    {
        var filters = new List<Tuple<string, string>>();
        foreach (var name in messageNames)
        {
            filters.Add(new Tuple<string, string>(name, "*"));
        }

        foreach (var filter in filters)
        {
            var labels = messageLabels[filter.Item1];

            foreach (var label in labels)
            {
                var fullLabel = filter.Item1 + "_" + label;
                messageColumns.Add(fullLabel);
            }

        }
    }

    void emitRow()
    {
        result.Add(extractions);
    }

    void parseMessage(MessageInfo messageInfo)
    {
        var unpackedData = buffer.Skip(pointer + MSG_HEADER_LEN).Take(messageInfo.Length - MSG_HEADER_LEN).ToArray();
        var convertedStruct = unpack(messageInfo.Structure, unpackedData);
        extractions = new Dictionary<string, string>(extractions);

        if (messageInfo.Name == "TIME")
        {
            emitRow();
        }

        int i = 0;
        foreach (var item in convertedStruct)
        {
            var convertedItem = item.ToString();
            if (messageInfo.Multipliers[i] != 1)
            {
                convertedItem = Convert.ToString(Convert.ToDouble(convertedItem) * messageInfo.Multipliers[i]);
            }

            var label = messageInfo.Labels[i];
            
            extractions[messageInfo.Name + "_" + label] = convertedItem;
            i++;
        }

        pointer += messageInfo.Length;
    }

    int bytesLeft()
    {
        return buffer.Length - pointer;
    }

    public string GenerateCSV(string delimiter = ",", string newLine = "\n")
    {
        // 2 lines CSV maker, LINQ FTW!
        var csv = String.Join(delimiter, messageColumns) + "\n";
        csv += String.Join(newLine, result.Select(row => String.Join(delimiter, messageColumns.Select(cell => row.ContainsKey(cell) ? row[cell] : ""))));
        
        //var allLines = new List<string>();
        //foreach(var item in result)
        //{
        //    var row = new List<string>();
        //    foreach(var column in messageColumns)
        //    {
        //        var cell = item.ContainsKey(column) ? item[column] : "";
        //        row.Add(cell);
        //    }

        //    allLines.Add(String.Join(delimiter, row));
        //}

        //csv += String.Join(newLine, allLines);

        return csv;
    }

    // This function unpacks a binary data according to python's unpack format
    // https://docs.python.org/2/library/struct.html#format-characters
    // Inspired by fdmillion's implementation https://stackoverflow.com/a/28418846/673606
    // My upgrade allows parsing of floats, characters, and variable length strings as well
    static object[] unpack(string fmt, byte[] bytes)
    {
        fmt = fmt.Replace(" ", "");
        
        if (fmt.Substring(0, 1) == "<")
        {
            fmt = fmt.Substring(1);
        }
        else if (fmt.Substring(0, 1) == ">")
        {
            fmt = fmt.Substring(1);
        }

        // disect the format into parts
        var parts = Regex.Matches(fmt, @"([0-9]+[A-Za-z])|[A-Za-z]").Cast<Match>().Select(m => m.ToString()).ToArray();

        List<Tuple<char, int>> parsedItems = new List<Tuple<char, int>>();
        
        int totalByteLength = 0;
        foreach (var part in parts)
        {

            var count = new String(part.TakeWhile(Char.IsDigit).ToArray());
            int number = 1;
            if (count.Length != 0)
            {
                number = Convert.ToInt32(count);
            }

            var partType = part[part.Length - 1];
            var partLength = getByteLength(partType.ToString()) * number;

            parsedItems.Add(new Tuple<char, int>(partType, partLength));

            totalByteLength += partLength;
        }
        
        // Test the byte array length to see if it contains as many bytes as is needed for the string.
        if (bytes.Length != totalByteLength) throw new ArgumentException("The number of bytes provided does not match the total length of the format string.");
        
        int byteArrayPosition = 0;
        List<object> outputList = new List<object>();
        byte[] buf;
        
        foreach (var item in parsedItems)
        {
            var itemLength = item.Item2;
            switch (item.Item1)
            {
                case 'q':
                    outputList.Add((object)(long)BitConverter.ToInt64(bytes, byteArrayPosition));
                    byteArrayPosition += itemLength;
                    break;
                case 'Q':
                    outputList.Add((object)(ulong)BitConverter.ToUInt64(bytes, byteArrayPosition));
                    byteArrayPosition += itemLength;
                    break;
                case 'f':
                    outputList.Add((object)(float)BitConverter.ToSingle(bytes, byteArrayPosition));
                    byteArrayPosition += itemLength;
                    break;
                case 'i':
                case 'I':
                case 'l':
                    outputList.Add((object)(int)BitConverter.ToInt32(bytes, byteArrayPosition));
                    byteArrayPosition += itemLength;
                    break;
                case 'L':
                    outputList.Add((object)(uint)BitConverter.ToUInt32(bytes, byteArrayPosition));
                    byteArrayPosition += itemLength;
                    break;
                case 'h':
                    outputList.Add((object)(short)BitConverter.ToInt16(bytes, byteArrayPosition));
                    byteArrayPosition += itemLength;
                    break;
                case 'H':
                    outputList.Add((object)(ushort)BitConverter.ToUInt16(bytes, byteArrayPosition));
                    byteArrayPosition += itemLength;
                    break;
                case 'b':
                    buf = new byte[1];
                    Array.Copy(bytes, byteArrayPosition, buf, 0, 1);
                    outputList.Add((object)(sbyte)buf[0]);
                    byteArrayPosition += itemLength;
                    break;
                case 'B':
                    buf = new byte[1];
                    Array.Copy(bytes, byteArrayPosition, buf, 0, 1);
                    outputList.Add((object)(byte)buf[0]);
                    byteArrayPosition += itemLength;
                    break;
                case 'x':
                    byteArrayPosition += itemLength;
                    break;
                case 's':
                case 'c':
                    buf = new byte[itemLength];
                    Array.Copy(bytes, byteArrayPosition, buf, 0, itemLength);
                    outputList.Add(System.Text.Encoding.UTF8.GetString(buf).Replace("\0", ""));
                    byteArrayPosition += itemLength;
                    break;
                default:
                    throw new ArgumentException("Unknown format detected.");
            }
        }
        return outputList.ToArray();
    }

    // Returns byte length based on its python type
    static int getByteLength(string type)
    {
        switch (type)
        {
            case "q":
            case "Q":
                return 8;
            case "i":
            case "I":
            case "f":
                return 4;
            case "h":
            case "H":
                return 2;
            case "b":
            case "B":
            case "x":
            case "s":
            case "c":
                return 1;
            default:
                throw new ArgumentException("Invalid character found in format.");
        }
    }

}
