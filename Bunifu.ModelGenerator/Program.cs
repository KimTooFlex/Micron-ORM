using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Micron;
using Micron.ModelGenerator.Properties;

namespace Bunifu.ModelGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string mode = "model-first";





            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            ConsoleColor defaultBgCol = Console.BackgroundColor;
            ConsoleColor defaultFore = Console.ForegroundColor;


            if (args.Length == 0)
            {

                Console.WriteLine(Resources.asciArt_txt);
                Console.WriteLine("A PRODUCT OF BUNIFU \n________________________________________\n\n\n");

                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("No project file added");

            }

            var projectPath = args[0];

            if (File.Exists(projectPath) && projectPath.ToLower().EndsWith(".csproj"))
            {
                MicronConfig config = new MicronConfig();
                FileInfo projectPathInfo = new FileInfo(projectPath.Trim());
                string[] ignoreList = new string[] { };

                string ignoreFile = Path.Combine(projectPathInfo.DirectoryName, "micron_ignore.txt");
                string modeFile = Path.Combine(projectPathInfo.DirectoryName, "micron_mode.txt");


                if (args.Length > 1)
                {
                    mode = args[1];
                    File.WriteAllText(modeFile, mode);
                }
                else
                {
                    mode = File.ReadAllText(modeFile);
                }


                if (File.Exists(ignoreFile)) ignoreList = File.ReadAllLines(ignoreFile);

                Console.WriteLine("Project File: " + projectPathInfo.FullName + "\n\n");
                string nameSpace = "Data.Models";

                if (!File.Exists(ignoreFile))
                {
                    File.WriteAllText(ignoreFile, "");

                    Console.WriteLine(Resources.asciArt_txt);
                    Console.WriteLine("A PRODUCT OF BUNIFU \n________________________________________\n\n\n");
                }
                else
                {

                    bool found = false;
                    Console.Write("Fetching connections: ");
                    //parse file
                    foreach (var file in projectPathInfo.Directory.GetFiles("*.cs", SearchOption.AllDirectories))
                    {
                        Console.Write("#");
                        //look for MicronConfig()
                        string source = File.ReadAllText(file.FullName);
                        source = source.Replace(" ", "");
                        source = source.Replace("\n", "");

                        if (source.Contains("MicronConfig()"))
                        {

                            if (source.Contains("DatabaseName=\""))
                            {
                                found = true;
                                Kuto k = new Kuto(source);
                                k = k.Extract("MicronConfig()", "");
                                k = k.Extract("{", "}");
                                if (k.Contains("DatabaseName")) config.DatabaseName = k.Extract("DatabaseName=\"", "\"").ToString();
                                if (k.Contains("Host")) config.Host = k.Extract("Host=\"", "\"").ToString();
                                if (k.Contains("Password")) config.Password = k.Extract("Password=\"", "\"").ToString();
                                if (k.Contains("User")) config.User = k.Extract("User=\"", "\"").ToString();
                                if (k.Contains("Locality")) config.Locality = k.Extract("Locality=\"", "\"").ToString();
                                break;
                            }
                        }
                        System.Threading.Thread.Sleep(300);
                    }

                    if (!found)
                    {
                        Console.WriteLine();
                        Console.WriteLine();

                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("No Micron Configuration found");
                        return;
                    }

                    Console.WriteLine();
                    Console.WriteLine();

                    Console.WriteLine("Micron Configuration found:");
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    MicronLogger.Log(config);
                    Console.BackgroundColor = defaultBgCol;
                }





                MicronDbContext db = new MicronDbContext(config);

                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine("Connected.... Generating DB Models for " + config.DatabaseName);
                Console.BackgroundColor = defaultBgCol;



                if (mode == "database-first")
                {
                    Console.WriteLine("Executng database-first");
                    //generate model here
                    db.GenerateModels(projectPath.Trim(), nameSpace, ignoreList);
                }
                else
                {
                    //model first
                    Console.WriteLine("Executng model-first");
                    db.GenerateSchemaFromModels(projectPath.Trim(), ignoreList);
                }





                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Successfully Completed :)");
                Console.BackgroundColor = defaultBgCol;
                Console.WriteLine();
                Console.WriteLine();
                Console.ForegroundColor = defaultFore;
                
                return;
            }


            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("No csProj found");
        }


    }
}



