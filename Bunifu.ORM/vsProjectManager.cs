using System;
using System.Collections.Generic;
using System.IO; 
namespace Micron
{
    public class vsProjectManager
    {
         
         

        public static void InstallToVisualStudio(string projectPath)
        {
            RepairFolder(projectPath);

            List<string> xml = new List<string>();
            FileInfo fi = new FileInfo(projectPath);
            string[] projectCodeLines = File.ReadAllLines(fi.FullName);
            string[] models = Directory.GetFiles(Path.Combine(fi.DirectoryName, "Models"));
            //remove old code 
            string xmlProject = "";
            foreach (var line in projectCodeLines)
            {
                if (!line.Contains("Models\\"))
                {
                   // xmlProject += line + "\n";
                    xml.Add(line);
                }
            }

            //if (xmlProject.Contains("</Compile>"))
            //{
            //    int i = xmlProject.IndexOf("</Compile>") + 10;
            //    if (i <= 10) return;
            //    foreach (var model in models)
            //    {
            //        FileInfo file = new FileInfo(model);
            //        i = xmlProject.IndexOf("</Compile>") + 10;
            //        if (i <= 10) break;
            //        xmlProject = xmlProject.Insert(i, "\n      <Compile Include=\"Models\\" + file.Name + "\" />");
            //    }
            //}

            bool added = false;
            foreach (var line in xml)
            {
                if (line.Trim().StartsWith("<Compile Include=") && !added)
                {
                    foreach (var model in models)
                    {
                        FileInfo file = new FileInfo(model);
                        xmlProject  += "      <Compile Include=\"Models\\" + file.Name + "\" />\n";
                    }
                    added = true;
                }
                xmlProject += line + "\n";
                 
            }

            File.WriteAllText(projectPath, xmlProject);         
        }

        public static void RepairFolder(string projectPath)
        {
            FileInfo fi = new FileInfo(projectPath);
            if(!Directory.Exists(Path.Combine(fi.DirectoryName, "Models"))) {
                Directory.CreateDirectory(Path.Combine(fi.DirectoryName, "Models"));
            }
        }

        public static void DeleteModelFiles(string projectPath)
        {
             FileInfo fi = new FileInfo(projectPath);
            var dir = Path.Combine(fi.DirectoryName, "Models");
            if (Directory.Exists(dir))
            {
                string[] models = Directory.GetFiles(dir);
                foreach (var model in models)
                {
                    File.Delete(model);
                }
            }

       
         
        }
    }
}