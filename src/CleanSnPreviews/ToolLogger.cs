using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace CleanSnPreviews
{
    public class ToolLogger
    {
        private bool _lineStart;
        private string _logFilePath;
        private string _logFolder = null;
        private int _count;
        private static string CR = Environment.NewLine;
        public string LogName { get; set; }

        private int _maxCount;
        public int MaxCount
        {
            get
            {
                if (_maxCount == 0)
                {
                    string maxCountFromconfig = ConfigurationManager.AppSettings["LogMaxRowCount"];
                    if (string.IsNullOrWhiteSpace(maxCountFromconfig) ||
                        !int.TryParse(maxCountFromconfig, out _maxCount))
                    {
                        _maxCount = 100000;
                    }
                }
                return _maxCount;
            }
        }

        public string LogFolder
        {
            get
            {
                if (_logFolder == null)
                    _logFolder = AppDomain.CurrentDomain.BaseDirectory;
                return _logFolder;
            }
            set
            {
                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);
                _logFolder = value;
            }
        }

        public void CreateLog(bool createNew, bool split = false)
        {
            _count = 0;
            _lineStart = true;
            string prevFilePath = _logFilePath;
            string startMessage = (split) ? $"Continuing preview cleaning log from previous file: {prevFilePath}" : "Start preview cleaning log";
            _logFilePath = Path.Combine(LogFolder, $"{LogName}_{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}.txt");
            if (!File.Exists(_logFilePath) || createNew)
            {
                using (FileStream fs = new FileStream(_logFilePath, FileMode.Create))
                {
                    using (StreamWriter wr = new StreamWriter(fs))
                    {
                        wr.WriteLine(startMessage);
                        wr.WriteLine();
                    }
                }
            }
            else
            {
                _count = File.ReadLines(_logFilePath).Count(); 
                LogWriteLine(CR, CR, "CONTINUING", CR, CR);
            }
        }

        public virtual void LogWrite(params object[] values)
        {
            using (StreamWriter writer = OpenLog())
            {
                WriteToLog(writer, values, false);
            }
            _lineStart = false;
        }

        public virtual void LogWriteLine(params object[] values)
        {
            using (StreamWriter writer = OpenLog())
            {
                WriteToLog(writer, values, true);
            }
            _lineStart = true;
        }

        private StreamWriter OpenLog()
        {
            if (_count >= this.MaxCount)
            {
                // We have to split log to a new file
                CreateLog(true, true);
            }
            return new StreamWriter(_logFilePath, true);
        }

        private void WriteToLog(StreamWriter writer, object[] values, bool newLine)
        {
            if (_lineStart)
            {
                Console.Write(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff\t"));
                writer.Write(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff\t"));
            }
            foreach (object value in values)
            {
                Console.Write(value);
                writer.Write(value);
            }
            if (newLine)
            {
                _count++;
                Console.WriteLine();
                writer.WriteLine();
            }
        }
    }
}