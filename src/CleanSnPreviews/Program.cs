using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.IO;
using System.Xml.XPath;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Permissions;
using System.Threading;


namespace CleanSnPreviews
{
    public enum PreviewCleanerModes
    {
        ShowOnly,
        Delete
    }

    class Program
    {
        // Console application parameters
        
        internal static List<string> ArgNames =
            new List<string>(new string[] {"MODE", "TOP", "WAIT", "?"});

        #region Usage screen
        private static string UsageScreen = String.Concat(
            //  0         1         2         3         4         5         6         7         |
            //  01234567890123456789012345678901234567890123456789012345678901234567890123456789|
            "Sense/Net Clean PreviewImage tool Usage:[br]",
            "CleanSnPreviews [-?] [-HELP][br]",
            "CleanSnPreviews [-MODE <mode>] [-TOP <top>] [-WAIT][br]",
            "[br]",
            "Parameters:[br]",
            "<mode>:         The operation mode, default is ShowOnly that will only show the first0 [br]",
            "                100 preview items. Delete will iterate all the preview images and delete it.[br][br]"
        );
        #endregion


        //start
        static void Main(string[] args)
        {
            Dictionary<string, string> parameters;
            string message;
            ParseParameters(args, ArgNames, out parameters, out message);
            PreviewCleaner cleanerInstance = new PreviewCleaner(parameters);
            cleanerInstance.logger.CreateLog(true);

            bool waitForAttach = parameters.ContainsKey("WAIT");
            bool help = parameters.ContainsKey("?") || parameters.ContainsKey("HELP");

            if (help)
            {
                Usage(message);
            }
            else
            {
                if (waitForAttach)
                {
                    cleanerInstance.logger.LogWriteLine(
                        "Running in wait mode - now you can attach to the process with a debugger.");
                    cleanerInstance.logger.LogWriteLine("Press ENTER to continue.");
                    cleanerInstance.logger.LogWriteLine();
                    Console.ReadLine();
                }

                try
                {
                    cleanerInstance.RunPreviewImageCleaner();
                }
                catch (Exception e)
                {
                    cleanerInstance.logger.LogWriteLine();
                    cleanerInstance.logger.LogWriteLine("========================================");
                    cleanerInstance.logger.LogWriteLine("CleanSnPreviews ends with error:");
                    cleanerInstance.logger.LogWriteLine(e.Message);
                }
                finally
                {
                    cleanerInstance.logger.LogWriteLine();
                    cleanerInstance.logger.LogWriteLine("========================================");
                    cleanerInstance.logger.LogWriteLine("Operation has ended.");
                }
            }
            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        internal static bool ParseParameters(string[] args, List<string> argNames,
            out Dictionary<string, string> parameters,
            out string message)
        {
            message = null;
            parameters = new Dictionary<string, string>();
            if (args.Length == 0)
                return false;

            int argIndex = -1;
            int paramIndex = -1;
            string paramToken = null;
            while (++argIndex < args.Length)
            {
                string arg = args[argIndex];
                if (arg.StartsWith("-"))
                {
                    paramToken = arg.Substring(1).ToUpper();

                    //if (paramToken == "?" || paramToken == "HELP")
                    //    return false;

                    paramIndex = ArgNames.IndexOf(paramToken);
                    if (!argNames.Contains(paramToken))
                    {
                        message = "Unknown argument: " + arg;
                        return false;
                    }
                    parameters.Add(paramToken, null);
                }
                else
                {
                    if (paramToken != null)
                    {
                        parameters[paramToken] = arg;
                        paramToken = null;
                    }
                    else
                    {
                        message = String.Concat("Missing parameter name before '", arg, "'");
                        return false;
                    }
                }
            }
            return true;
        }

        private static void Usage(string message)
        {
            if (!String.IsNullOrEmpty(message))
            {
                Console.WriteLine("--------------------");
                Console.WriteLine(message);
                Console.WriteLine("--------------------");
            }
            Console.WriteLine(UsageScreen);
        }

    }

}