using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Micron;

namespace TestProject
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            MicronConfig config = new MicronConfig()
            {
                DatabaseName = "books_db",
               
            };
            MicronDbContext.AddConnectionSetup(config);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}


