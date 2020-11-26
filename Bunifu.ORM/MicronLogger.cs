using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Micron
{
   public static class MicronLogger
    {
        /// <summary>
        /// Creates a string of all property value pair in the provided object instance
        /// </summary>
        /// <param name="objectToGetStateOf"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static string Log(object objectToGetStateOf)
        {
            if (objectToGetStateOf == null)
            {
                Console.WriteLine(objectToGetStateOf);
                return "";
            }
            var builder = new StringBuilder();

            foreach (var property in objectToGetStateOf.GetType().GetProperties())
            {
                try
                {
                    object value = property.GetValue(objectToGetStateOf, null);

                    builder.Append(property.Name)
                    .Append(" = ")
                    .Append((value ?? "null"))
                    .AppendLine();
                }
                catch (Exception)
                { 
                }
            }
            
            Console.WriteLine(builder.ToString());
            return builder.ToString();
        }
    }
}
