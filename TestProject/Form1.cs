using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Design.PluralizationServices;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Micron;
using Data.Models;
using System.Collections;

namespace TestProject
{
    public partial class Form1 : Form
    {

        MicronDbContext db = new MicronDbContext();
        public Form1()
        {
            db.DebugMode = true;
            InitializeComponent();

        }

          
        private void Button1_Click(object sender, EventArgs e)
        {


            var a = db.GetRecord<Author>();

            //MessageBox.Show(MicronLogger.Log(testBook.GetAuthor().geta));
            //MessageBox.Show( );




        }

        private void Button2_Click(object sender, EventArgs e)
        {
             
        }
    }


}
