using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Micron
{
    public static class MicronExtensions
    {
        public static IEnumerable<T> For<T>(this IEnumerable<T> me, IMicron micron)
        {
            if (micron == null) return new List<T>();
            MicronDbContext db = (MicronDbContext)GetPropValue(micron, "DefaultDBContext");
            var table1 = db.GetTableName(typeof(T)); //order
            var table2 = db.GetTableName(micron.GetType()); //customer
            List<T> ret = new List<T>();

            try
            {
                //get relationship property
                var relationships = db.Relationships.AsEnumerable()
                         .Where(r =>
                         r.Field<string>("REFERENCED_TABLE_NAME").ToLower() == table2.ToLower()
                         &&
                         r.Field<string>("TABLE_NAME").ToLower() == table1.ToLower()

                         )?.CopyToDataTable();
                var table1Col = relationships.Rows[0]["COLUMN_NAME"].ToString();
                var table2Col = relationships.Rows[0]["REFERENCED_COLUMN_NAME"].ToString();

                var val = GetPropValue(micron, table2Col);
                { }
                foreach (var item in me)
                {
                    var val2 = GetPropValue(item, table1Col);
                    //filter here
                    if (val2.ToString() == val.ToString())
                    {
                        ret.Add(item);
                    }

                }
                return ret;
            }
            catch (Exception)
            {
                return new List<T>();
            }


        }
        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }
    }


}
