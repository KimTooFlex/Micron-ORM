using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Micron
{
    public static class MicronObjectMapper
    {

        /// <summary>
        /// Converts a DataTable to a list with generic objects
        /// </summary>
        /// <typeparam name="T">Generic object</typeparam>
        /// <param name="datatable">DataTable</param>
        /// <returns>List with generic objects</returns>
        public static IEnumerable<T> MapToList<T>(DataTable datatable) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();

                foreach (var row in datatable.AsEnumerable())
                {
                    T obj = new T();
                    foreach (var prop in obj.GetType().GetProperties())
                    {

                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                            MicronDbContext db = (MicronDbContext)datatable.ExtendedProperties["db"];

                            if (prop.GetValue(obj, null) != null && prop.GetValue(obj, null).GetType() == typeof(DateTime))
                            {
                                propertyInfo.SetValue(obj, row[prop.Name], null);
                            }
                            else if (prop.PropertyType == typeof (MicronDbContext))
                            {

                                propertyInfo.SetValue(obj,db, null);
                            }
                            else
                            {
                                propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);

                            }

                             

                        }
                        catch (Exception err)
                        {
                            continue;
                        }
                    }


                    list.Add(obj);
                }



                return list;
            }
            catch
            {
                return null;
            }
        }



        static IEnumerable<T> getRows<T>() where T : class, new()
        {
            return null;
        }


        public static IEnumerable<T> MapToList<T>(DataView dataview) where T : class, new()
        {
            return MapToList<T>(dataview.ToTable());
        }

        public static T MapToOnject<T>(DataView dataview) where T : class, new()
        {
            var items = MapToList<T>(dataview.ToTable());
            if (items.Count() > 0)
            {
                return items.ToList()[0];
            };
            return null;
        }

    }
}
