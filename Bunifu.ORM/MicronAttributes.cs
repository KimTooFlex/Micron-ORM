using System;
using System.Linq;

namespace Micron
{
    public static class AttributeExtensions
    {
        public static TValue GetAttributeValue<TAttribute, TValue>(
            this Type type,
            Func<TAttribute, TValue> valueSelector)
            where TAttribute : Attribute
        {
            var att = type.GetCustomAttributes(
                typeof(TAttribute), true
            ).FirstOrDefault() as TAttribute;
            if (att != null)
            {
                return valueSelector(att);
            }
            return default(TValue);
        }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        private string tableName;

        public TableAttribute(string tableName)
        {
            this.tableName = tableName;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryAttribute : Attribute
    {
        public string FieldName;
        public PrimaryAttribute()
        {

        }
        public PrimaryAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

       
    }
 
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignAttribute : Attribute
    {
        private Type modelObject; 

        
        public ForeignAttribute(Type modelObject)
        {
            this.modelObject = modelObject; 
        }

    }

    public interface IMicron
    {
       // MicronDbContext DefaultDBContext { get; set; }
    }
    public class Micron
    {
        public object Data { get; set; } 
    }
    public class Microns
    {
        public object Data { get; set; } 
    }
}