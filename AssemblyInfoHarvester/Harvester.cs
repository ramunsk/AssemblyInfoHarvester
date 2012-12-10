using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AssemblyInfoHarvester
{
    public class Harvester
    {
        private Version _assemblyVersion;
        private Version _assemblyFileVersion;
        private List<string> _files;
        private string _directory;

        private Regex _assemblyVersionRx;
        private Regex _assemblyFileVersionRx;


        public Harvester()
        {
            _assemblyVersionRx = new Regex(@"(?<=\[assembly:\s*AssemblyVersion\(\"")(\d+.){3}(\d+)(?=\""\)\])");
            _assemblyFileVersionRx = new Regex(@"(?<=\[assembly:\s*AssemblyFileVersion\(\"")(\d+.){3}(\d+)(?=\""\)\])");
        }

        private static string GetArgumentValue(IEnumerable<string> args, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return args.FirstOrDefault(x => !x.StartsWith("/"));
            }

            var arg = args.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase));
            return arg == null ? null : arg.Substring(prefix.Length).Replace("\"", "");
        }

        public void Harvest(string[] args)
        {
            PrintLogo();

            if (args == null || args.Length == 0)
            {
                PrintHelp();
                Exit(0);
            }

            ParseAssembyVersion(args);
            ParseAssembyFileVersion(args);
            ParseFiles(args);
            ParseDirectory(args);

            PrintVariables();

            DoHarvest();
        }

        private void PrintVariables()
        {
            Console.WriteLine("{0,-25}{1, -54}", "AssemblyVersion:", _assemblyVersion);
            Console.WriteLine("{0,-25}{1, -54}", "AssemblyFileVersion:",
                              _assemblyFileVersion == null ? "<null>" : _assemblyFileVersion.ToString());

            Console.Write("{0,-25}", "Files:");
            _files.ForEach(x => Console.Write("[{0}] ", x));
            Console.Write("\r\n");

            Console.WriteLine("{0,-25}{1, -54}", "Directory:", _directory);
            Console.WriteLine();
        }



        private void ParseFiles(IEnumerable<string> args)
        {
            var value = GetArgumentValue(args, "/f:");

            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine("[/f] - attrubute not specified");
                Exit(1);
            }

            _files = value.Split(';').ToList();
        }

        private void ParseDirectory(IEnumerable<string> args)
        {
            var value = GetArgumentValue(args, null);
            if (string.IsNullOrEmpty(value))
            {
                value = Directory.GetCurrentDirectory();
            }

            _directory = value;

        }

        private void ParseAssembyFileVersion(IEnumerable<string> args)
        {
            var value = GetArgumentValue(args, "/afv:");

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            Version ver;
            if (!Version.TryParse(value, out ver))
            {
                Console.WriteLine("[/afv] - {0} is not a valid version number", value);
                Exit(1);
            }
            _assemblyFileVersion = ver;
        }

        private void ParseAssembyVersion(IEnumerable<string> args)
        {
            var value = GetArgumentValue(args, "/av:");
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine("[/av] - attrubute not specified");
                Exit(1);
            }

            Version ver;
            if (!Version.TryParse(value, out ver))
            {
                Console.WriteLine("[/av] - {0} is not a valid version number", value);
                Exit(1);
            }
            _assemblyVersion = ver;
        }

        private static void PrintLogo()
        {
            Console.WriteLine("AssemblyInfo Harvester");
            Console.WriteLine("A tool to change AssemblyVersion and AssemblyFileVersion attribute values");
            Console.WriteLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("{0} <options> [<directory>]", Path.GetFileName(Assembly.GetExecutingAssembly().Location));
            Console.WriteLine();
            Console.WriteLine("options:");
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "/av:\"<version>\"", "Required");
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "", "A version to set for AssemblyVersion attribute");
            Console.WriteLine();
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "/afv:\"<version>\"", "Optional");
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "", "A version to set for AssemblyFileVersion attribute");
            Console.WriteLine();
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "/f:\"<file(s)>\"", "Required");
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "", "Names of file to look for attributes in");
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "", "Separate multiple file names with semi-colon (;)");
            Console.WriteLine();
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "<directory>", "Required");
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "", "A path to start harvesting from");
            Console.WriteLine("{0,-3}{1,-20}{2,-55}", "", "",
                              "If not specified harvesting will start from current directory");
        }

        private static void Exit(int exitCode)
        {
            Console.ReadLine();
            Environment.Exit(exitCode);
        }

        private void DoHarvest()
        {
            var path = _directory;

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path);
            }

            foreach(var file in _files)
            {
                var list = Directory.GetFiles(path, file, SearchOption.AllDirectories);

                foreach(var item in list)
                {
                    var content = File.ReadAllText(item);
                    content = _assemblyVersionRx.Replace(content, _assemblyVersion.ToString());

                    if (_assemblyFileVersion != null)
                    {
                        content = _assemblyFileVersionRx.Replace(content, _assemblyFileVersion.ToString());
                    }

                    File.WriteAllText(item, content);

                    Console.WriteLine(item);
                }
            }
            Exit(0);
        }
    }
}