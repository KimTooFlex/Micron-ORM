using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Design.PluralizationServices;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Micron
{

    [Serializable]
    public class MicronConfig
    {
        public string Host { get; set; } = "localhost";
        public string DatabaseName { get; set; } = "information_schema";
        public string User { get; set; } = "root";
        public string Password { get; set; } = "";
        public string Port { get; set; } = "3306";
        public string Locality { get; set; } = "en-us";

    }
#if RELEASE
    [DebuggerStepThrough]
#endif
    public class MicronDbContext
    {

        public bool DebugMode { get; set; } = false;


        private MySqlConnection mysqlConnection;
        private Exception lastException;
        private int affectedRecords;
        private object lastInsertedID;
        private string lastQuery;
        private string after_edit;
        private string before_edit;
        private MySqlTransaction transaction = null;

        public static Dictionary<string, MicronConfig> setups = new Dictionary<string, MicronConfig>();
        public DataTable Relationships;
        private DataRowCollection Tables;
        private string Locality;

        public static void AddConnectionSetup(MicronConfig setup, object key = null)
        {
            if (key == null)
            {
                key = "Default";
            }
            setups[key.ToString()] = setup;
        }
        public static void SetAutomaticMigration(bool arg = true)
        {
            if (arg)
            {
                var db = new MicronDbContext();
                string hash = db.GetDatabaseHash();

                if (File.Exists("micron_db_ver.txt"))
                {
                    string phash = File.ReadAllText("micron_db_ver.txt");
                    if (hash != phash)
                    {
                        db.UpdateDatabase();
                        System.Threading.Thread.Sleep(5000);
                    }
                }
                else
                {
                    db.UpdateDatabase();
                    System.Threading.Thread.Sleep(5000);
                }
                File.WriteAllText("micron_db_ver.txt", db.GetDatabaseHash());
            }
        }

        void LoadRelationships(string db)
        {
            this.Relationships = this.Query("SELECT TABLE_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE REFERENCED_TABLE_SCHEMA = '" + db + "'");
            this.Tables = this.Query("SHOW TABLES").Rows;
        }

        public MicronDbContext(string databaseName, string host = "localhost", string user = "root", string password = "", string port = "3306")
        {
            try
            {
                this.Locality = "en-us";
                string ConectionString = "SERVER=" + host + ";" + "DATABASE=" + databaseName + ";" + "UID=" + user + ";" + "PASSWORD=" + password + ";";
                this.mysqlConnection = new MySqlConnection(ConectionString);
                OpenConnection();
                LoadRelationships(databaseName);
            }
            catch (Exception err)
            {
                this.lastException = err;
            }
        }
        public MicronDbContext(MicronConfig setup)
        {
            try
            {

                this.Locality = setup.Locality;
                string ConectionString = "SERVER=" + setup.Host + ";" + "DATABASE=" + setup.DatabaseName + ";" + "UID=" + setup.User + ";" + "PASSWORD=" + setup.Password + ";";
                this.mysqlConnection = new MySqlConnection(ConectionString);
                OpenConnection();
                LoadRelationships(setup.DatabaseName);
            }
            catch (Exception err)
            {
                this.lastException = err;
            }
        }

        public MicronDbContext()
        {
            //check resources


            if (MicronDbContext.setups.Keys.Count == 0)
            {
                throw new Exception("No connection setup defined");
            }
            try
            {
                MicronConfig setup = MicronDbContext.setups.Values.FirstOrDefault();
                this.Locality = setup.Locality;
                string ConectionString = "SERVER=" + setup.Host + ";" + "DATABASE=" + setup.DatabaseName + ";" + "UID=" + setup.User + ";" + "PASSWORD=" + setup.Password + ";";
                this.mysqlConnection = new MySqlConnection(ConectionString);
                OpenConnection();
                LoadRelationships(setup.DatabaseName);
            }
            catch (Exception err)
            {
                this.lastException = err;
            }
        }

        public MicronDbContext(object setupName)
        {
            if (!MicronDbContext.setups.ContainsKey(setupName.ToString()))
            {
                throw new Exception(setupName + "connection setup not defined");
            }
            try
            {
                MicronConfig setup = MicronDbContext.setups[setupName.ToString()];
                this.Locality = setup.Locality;
                string ConectionString = "SERVER=" + setup.Host + ";" + "DATABASE=" + setup.DatabaseName + ";" + "UID=" + setup.User + ";" + "PASSWORD=" + setup.Password + ";";
                this.mysqlConnection = new MySqlConnection(ConectionString);
                OpenConnection();
                LoadRelationships(setup.DatabaseName);
            }
            catch (Exception err)
            {
                this.lastException = err;
            }
        }

        public string GetDatabaseHash()
        {
            StringBuilder txt = new StringBuilder();
            foreach (DataRow item in Query("SHOW TABLES").Rows)
            {
                txt.Append(item[0].ToString());
                //loop cols and 
                foreach (DataRow col in Query($"SHOW COLUMNS FROM `{item[0].ToString()}`").Rows)
                {
                    txt.Append(col[0].ToString());
                    txt.Append(col[1].ToString());
                    txt.Append(col[2].ToString());
                    txt.Append(col[3].ToString());
                    txt.Append(col[4].ToString());
                    txt.Append(col[5].ToString());
                }
            }

            //loop relationships
            foreach (DataRow row in Relationships.Rows)
            {
                for (int i = 0; i < Relationships.Columns.Count; i++)
                {
                    txt.Append(row[i].ToString());
                }
            }

            return CreateMD5(txt.ToString());
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public void UpdateDatabase()
        {

            var interfae = typeof(IMicron);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfae.IsAssignableFrom(p));
            List<string> newTables = new List<string>();
            foreach (var type in types)
            {

                if (type == typeof(IMicron)) continue;
                var tableName = GetTableName(type);
                newTables.Add(tableName);
                var info = GetPrimaryKey(type);

                IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
                string sql = "";
                if (!isTableExists(tableName))
                {
                    Console.WriteLine($"Creating table {tableName}");
                    //create table 
                    Query($"CREATE TABLE  `{tableName}` ( `{info.PrimaryKeyName}` INT NOT NULL AUTO_INCREMENT, PRIMARY KEY (`{info.PrimaryKeyName}`)) ENGINE = InnoDB;");
                }

                List<string> newCols = new List<string>();
                foreach (PropertyInfo prop in props)
                {
                    if (prop.PropertyType == typeof(MicronDbContext)) continue;
                    string colName = prop.Name;
                    newCols.Add(colName.ToLower());
                    string colType = getMySQLType(prop.PropertyType);
                    if (isColumnExists(tableName, colName))
                    {
                        Console.WriteLine($"Updating column {colName}");
                        //update if any
                        //Query($"ALTER TABLE `{tableName}` CHANGE `{colName}` `{colName}` {colType} NOT NULL");
                    }
                    else
                    {

                        Console.WriteLine($"Creating column {colType}");
                        //create column
                        Query($"ALTER TABLE `{tableName}` ADD `{colName}` {colType} NOT NULL");
                    }

                    //if index
                    if (prop.GetCustomAttributes(false).Count() > 0 && prop.GetCustomAttributes(false)[0].GetType() == typeof(ForeignAttribute))
                    {
                        Query($"ALTER TABLE `{tableName}` ADD INDEX(`{colName}`)");
                    }


                }

                //delete missing columns
                DataRowCollection cols = this.Query("SHOW COLUMNS FROM `" + tableName + "`").Rows;
                foreach (DataRow tableInfo in cols)
                {
                    try
                    {
                        if (!newCols.Contains(tableInfo[0].ToString().ToLower()))
                        {
                            Console.WriteLine($"Dropping column {tableInfo[0].ToString()}");
                            //dropc 
                            Query($"ALTER TABLE `{tableName}` DROP `{tableInfo[0].ToString()}`");
                        }
                    }
                    catch (Exception)
                    {

                    }
                }




            }

            //drop missing tables here

            foreach (DataRow table in Tables)
            {
                if (newTables.Where(r => r.ToLower() == table[0].ToString().ToLower()).Count() == 0)
                {
                    //droping table
                    Console.WriteLine($"Dropping table {table[0].ToString()}");
                    Query($"DROP TABLE `{table[0].ToString()}`");

                }
            }

            //find relationships
            foreach (var type in types)
            {

                if (type == typeof(IMicron)) continue;
                var tableName = GetTableName(type);
                var info = GetPrimaryKey(type);

                IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
                foreach (PropertyInfo prop in props)
                {
                    if (prop.GetCustomAttributes(false).Count() > 0 && prop.GetCustomAttributes(false)[0].GetType() == typeof(ForeignAttribute))
                    {
                        string colName = prop.Name;
                        var table2Typ = (Type)prop.GetCustomAttributesData()[0].ConstructorArguments[0].Value;
                        string table2 = GetTableName(table2Typ);
                        var table2Info = GetPrimaryKey(table2Typ);
                        Query($"ALTER TABLE `{tableName}` ADD FOREIGN KEY (`{colName}`) REFERENCES `{table2}`(`{table2Info.PrimaryKeyName}`) ON DELETE RESTRICT ON UPDATE RESTRICT;");
                    }
                }
            }
        }

        string getMySQLType(Type type)
        {
            string ret = "TEXT";

            if (type == typeof(int)) ret = "INT";
            if (type == typeof(decimal)) ret = "DECIMAL";
            if (type == typeof(DateTime)) ret = "DATETIME";
            if (type == typeof(float)) ret = "FLOAT";
            if (type == typeof(double)) ret = "DOUBLE";
            if (type == typeof(bool)) ret = "BOOLEAN";

            return ret;
        }

        ~MicronDbContext()
        {
            this.CloseConnection();
        }


        public IDbTransaction BeginTransaction()
        {
            this.transaction = this.mysqlConnection.BeginTransaction();
            return this.transaction;
        }

        public void ClearTransaction()
        {
            this.transaction = null;
        }

        public IDbTransaction GetTransaction()
        {

            return this.transaction;
        }

        public void CommitTransaction()
        {
            if (this.transaction != null)
            {
                this.transaction.Commit();
            }
        }
        public void RollbackTransaction()
        {
            if (this.transaction != null)
            {
                this.transaction.Rollback();
                this.transaction = null;
            }
        }

        public int Exec(string querry)
        {
            try
            {
                MySqlCommand command = this.mysqlConnection.CreateCommand();
                command.CommandText = querry;


                if (this.transaction != null)
                {
                    command.Transaction = this.transaction;
                }

                if (this.DebugMode) Console.WriteLine(querry);

                this.affectedRecords = command.ExecuteNonQuery();
                this.lastInsertedID = command.LastInsertedId;
                this.lastQuery = querry;
                return this.GetAffectedRecords();
            }
            catch (Exception err)
            {
                setError(err);
                return 0;
            }
        }




        public IEnumerable<T> GetRecords<T>(object[] identities) where T : class, IMicron, new()
        {
            try
            {
                string tablename = GetTableName(typeof(T));
                var pkProps = (GetPrimaryKey(typeof(T)));
                string where = string.Empty;
                if (tablename.Length > 0 && identities.Length > 0)
                {
                    string pk = pkProps.PrimaryKeyName;
                    foreach (var identity in identities)
                    {
                        where += "`" + pk + "`='" + identity + "' OR";
                    }
                    where = "(" + where.Substring(0, where.Length - 2) + ")";
                    return GetRecords<T>("SELECT * FROM `" + tablename + "` WHERE " + where + ";");
                }
            }
            catch (Exception err)
            {
                setError(err);
            }

            return null;

        }
        public IEnumerable<T> GetRecords<T>(string queryOrTableName, object[] identities) where T : class, IMicron, new()
        {
            try
            {
                string tablename = GetTableName(typeof(T));
                var pkProps = GetPrimaryKey(typeof(T));
                string where = string.Empty;

                if ((!isSelectQuery(tablename) && tablename.Length > 0) && identities.Length > 0)
                {
                    string sql = queryOrTableName.Trim();
                    if (!isSelectQuery(queryOrTableName))
                    {
                        sql = "SELECT * FROM `" + GetTableName(typeof(T)) + "`";
                    }

                    if (sql.ToLower().Contains("where"))
                    {
                        where += " AND (";
                    }
                    else
                    {
                        where += " WHERE (";
                    }

                    string pk = pkProps.PrimaryKeyName;
                    foreach (var identity in identities)
                    {
                        where += "`" + pk + "`='" + identity + "' OR";
                    }
                    where = where.Substring(0, where.Length - 2) + ")";


                    if (sql.ToLower().Contains(" order by "))
                    {
                        sql = sql.Insert(sql.ToLower().IndexOf(" order by "), where);
                    }
                    else if (sql.ToLower().Contains(" limit "))
                    {
                        sql = sql.Insert(sql.ToLower().IndexOf(" limit "), where);
                    }
                    else
                    {
                        sql = sql + where + ";";
                    }

                    return GetRecords<T>(sql);
                }
            }
            catch (Exception err)
            {
                setError(err);
            }

            return null;

        }



        public T Fresh<T>(T micronModel, string tableName = null) where T : class, IMicron, new()
        {
            if (tableName == null)
            {
                tableName = GetTableName(typeof(T));
            }
            PrimaryKeyProperties pkProp = GetPrimaryKey(micronModel);
            return GetRecord<T>(pkProp.PrimaryKeyValue);
        }

        bool isSelectQuery(string queryOrTableName)
        {
            return (queryOrTableName.ToLower().Trim().StartsWith("select ") || queryOrTableName.ToLower().Trim().StartsWith("show "));
        }


        public IEnumerable<T> GetRecords<T>(object arg) where T : class, IMicron, new()
        {
            string sql = string.Empty;
            var tableInfo = GetPrimaryKey(typeof(T));

            //detect micron
            if (arg.GetType().GetInterfaces().Contains(typeof(IMicron)))
            {
                if (typeof(T) == arg.GetType())
                {
                    string q = "true";
                    //loop all properties
                    foreach (PropertyInfo prop in arg.GetType().GetProperties())
                    {
                        var type = prop.PropertyType;
                        if (prop.GetValue(arg, null) != null && prop.GetValue(arg, null).GetType() == typeof(string))
                        {
                            q += $" AND `{prop.Name}` LIKE '{prop.GetValue(arg, null)}'";
                        }

                    }
                    arg = q;
                }
                else
                {
                    //fetch rlationships here
                    throw new Exception("PROVIDED MICRON OBJECT IS NOT OF SAME TYPE");
                }

            }


            try
            {

                if (isSelectQuery(arg.ToString()))
                {
                    sql = arg.ToString();
                }
                else
                {
                    if (arg.GetType().IsArray)
                    {
                        //its collection
                        string w = "";

                        foreach (var item in (Array)arg)
                        {
                            w += "`" + tableInfo.PrimaryKeyName + "`='" + item + "' OR";
                        }
                        w = w.Trim().Substring(0, w.Trim().Length - 3);
                        if (w.Trim().Length > 0) sql = "SELECT * FROM `" + GetTableName(typeof(T)) + "` WHERE " + w + ";";
                    }
                    else
                    {
                        //it is a 
                        sql = "SELECT * FROM `" + GetTableName(typeof(T)) + "` WHERE " + arg + ";";
                    }
                }



                if (this.DebugMode) Console.WriteLine(sql);
                MySqlDataAdapter dataAdpter = new MySqlDataAdapter(sql, this.mysqlConnection);
                DataSet dataset = new DataSet();
                dataAdpter.Fill(dataset);
                this.lastQuery = sql;
                if (dataset.Tables[0].Columns.Count > 0)
                {
                    DataTable tbl = dataset.Tables[0];
                    tbl.ExtendedProperties.Add("db", this);
                    return MicronObjectMapper.MapToList<T>(tbl);

                }
                return null;
            }
            catch (Exception err)
            {
                setError(err);
                return null;
            }
        }
        public IEnumerable<T> GetRecords<T>() where T : class, IMicron, new()
        {
            try
            {
                string tablename = GetTableName(typeof(T));

                if (tablename.Length > 0)
                {
                    return GetRecords<T>("SELECT * FROM `" + tablename + "`;");
                }
            }
            catch (Exception err)
            {
                setError(err);
            }

            return null;
        }



        void setError(Exception err)
        {
            Console.WriteLine(err.Message);
            this.lastException = err;
        }
        public DataTable Query(string query)
        {
            try
            {
                string sql = query.Trim();
                MySqlDataAdapter dataAdpter = new MySqlDataAdapter(sql, this.mysqlConnection);
                DataSet dataset = new DataSet();
                dataAdpter.Fill(dataset);
                this.lastQuery = sql;
                if (DebugMode) Console.WriteLine(sql);
                return dataset.Tables[0];
            }
            catch (Exception err)
            {
                setError(err);
                return null;
            }
        }
        public DataTable Query<T>()
        {
            try
            {
                string sql = "SELECT * FROM `" + GetTableName(typeof(T)) + "`";
                MySqlDataAdapter dataAdpter = new MySqlDataAdapter(sql, this.mysqlConnection);
                DataSet dataset = new DataSet();
                dataAdpter.Fill(dataset);
                this.lastQuery = sql;
                return dataset.Tables[0];
            }
            catch (Exception err)
            {
                setError(err);
                return null;
            }
        }
        bool containsSpecialCharacters(string text)
        {
            if (text.Trim().Contains(" ")) return true;
            var withoutSpecial = new string(text.Trim().Where(c => Char.IsLetterOrDigit(c)
                                              || Char.IsWhiteSpace(c)).ToArray());
            return (text.Trim() != withoutSpecial);
        }

        object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public T GetRecord<T>(object arg = null) where T : class, IMicron, new()
        {



            try
            {


                string sql = string.Empty;
                var tableinfo = GetPrimaryKey(typeof(T));
                string table = GetTableName(typeof(T));


                if (arg == null)
                {
                    sql = "SELECT * FROM `" + table + "` LIMIT 1;";
                }
                else if (isSelectQuery(arg.ToString()))
                {
                    //its collection
                    sql = arg.ToString();
                }
                else if (containsSpecialCharacters(arg.ToString()))
                {
                    ///its where
                    sql = "SELECT * FROM `" + table + "` WHERE(" + arg.ToString().Trim() + ") LIMIT 1;";
                }
                else
                {
                    //its identity
                    sql = "SELECT * FROM `" + table + "` WHERE `" + tableinfo.PrimaryKeyName + "`='" + arg.ToString().Trim() + "' LIMIT 1;";
                }


                var a = GetRecords<T>(sql);
                foreach (var item in a)
                {
                    return item;
                    break;
                }
            }
            catch (Exception)
            {

            }
            return null;
        }


        public T Save<T>(T modelObject, string tableName = null) where T : class, IMicron, new()
        {
            try
            {

                string table = tableName;
                if (table == null)
                {
                    table = GetTableName(typeof(T));
                }

                var pkProp = GetPrimaryKey(modelObject);

                if (pkProp.PrimaryKeyValue.ToString() == "0")
                {
                    Exec(GenerateInsert<T>(modelObject, table));
                    return GetRecord<T>(GetLastInsertID());
                }
                else
                {
                    Exec(GenerateUpdate<T>(modelObject, table));
                    return GetRecord<T>(GetPrimaryKey(modelObject).PrimaryKeyValue);
                }


            }
            catch (Exception err)
            {
                setError(err);
                return null;
            }
        }
        public bool Delete<T>(T micronModel, string tableName = null, object transactionID = null) where T : class, IMicron, new()
        {
            if (tableName == null)
            {
                tableName = GetTableName(typeof(T));
            }

            return Exec(GenerateDelete<T>(micronModel, tableName)) > 0;
        }





        public void GenerateModels(string csprojProjectPath = null, string nameSpace = "Data.Models", string[] ignoreList = null)
        {
            if (csprojProjectPath != null)
            {
                vsProjectManager.DeleteModelFiles(csprojProjectPath);
                vsProjectManager.RepairFolder(csprojProjectPath);
            }

            foreach (DataRow table in Tables)
            {
                if (ignoreList.Contains(table[0].ToString()))
                {
                    Console.WriteLine("IGNORED " + table[0]);
                    continue;
                }

                string txt = GenerateModel(table[0].ToString(), nameSpace, ignoreList);
                if (csprojProjectPath != null)
                {
                    FileInfo fi = new FileInfo(csprojProjectPath);
                    File.WriteAllText(Path.Combine(fi.DirectoryName, "Models/" + PascalCase(table[0].ToString()) + ".cs"), txt);
                }
            }
            if (csprojProjectPath != null)
            {
                FileInfo fi = new FileInfo(csprojProjectPath);
                File.WriteAllText(Path.Combine(fi.DirectoryName, "Models/__Relationships.cs"), GenerateRelationships(nameSpace, ignoreList));
            }
            vsProjectManager.InstallToVisualStudio(csprojProjectPath);

        }

        public void GenerateSchemaFromModels(string csprojProjectPath = null, string[] ignoreList = null)
        {

            //access models 
            string nameSpace = "Data.Models";
            var projFileInfo = new FileInfo(csprojProjectPath);
            var modelsPath = Path.Combine(projFileInfo.DirectoryName, "Models");
            if (!Directory.Exists(modelsPath))
            {
                Console.WriteLine($"{projFileInfo.Directory.Name}/Models folder missing");
                return;
            }
            //scrap schema
            List<string> new_tables = new List<string>();
            Dictionary<string, string> RelationshipsQuery = new Dictionary<string, string>();

            Dictionary<string, string> tableInfo = new Dictionary<string, string>();

            foreach (var file in Directory.GetFiles(modelsPath, "*.cs"))
            {
                var fileInfo = new FileInfo(file);
                var code = File.ReadAllText(file);
                if (!code.Replace(" ", "").Contains(":IMicron")) continue;
                while (code.Contains("  ")) code = code.Replace("  ", " ");
                Kuto kuto = new Kuto(Regex.Replace(code, @"\t|\n|\r", ""));
                while (kuto.Contains("IMicron"))
                {
                    //get table
                    string objectName = kuto.Extract("class", ":").ToString().Trim();
                    var tableName = kuto.Extract("[Table(\"", "\"").ToString().Trim();

                    nameSpace = kuto.Extract("namespace", "{").ToString().Trim();
                    if (tableName.Length == 0)
                    {
                        Console.WriteLine($"Table attribute missing in {fileInfo.Name}");
                        continue;
                    }
                    var tableExists = isTableExists(tableName, true);
                    Console.WriteLine($"{(tableExists ? "Updating" : "Creating")} {tableName.ToUpper()}");
                    new_tables.Add(tableName.ToLower());


                    //create tables here
                    kuto = kuto.Extract("IMicron", "");
                    string[] props = kuto.ToString().Split(new char[] { '}' });
                    string sql = $"CREATE TABLE `{tableName}` (";
                    string primaryKey = "";
                    string indexes = "";
                    List<string> columns = new List<string>();
                    foreach (var prop in props)
                    {
                        var propText = prop.Trim();
                        if (propText.Length == 0) break;
                        if (propText.StartsWith("//")) continue;
                        if (propText.StartsWith("/*")) continue;
                        if (propText.Replace(" ", "").Contains(":IMicron")) break;


                        var K2 = new Kuto(propText).Extract(" ", "").Trim();

                        string propName = Kuto.Reverse(new Kuto(Kuto.Reverse(propText)).Extract("{", "").Extract("", " ").ToString().Trim());
                        string propTypeString = Kuto.Reverse(new Kuto(Kuto.Reverse(propText)).Extract("{", "").Extract(" ", "").Extract("", " ").ToString().Trim());
                        if (propTypeString == "int") propTypeString = "Int32";
                        var propType = Type.GetType("System." + PascalCase(propTypeString));

                        var colSQL = $"ALTER TABLE `{tableName}` ADD `{propName}` {getMySQLType(propType)} NOT NULL";

                        if (tableExists && isColumnExists(tableName, propName))
                        {
                            colSQL = $"ALTER TABLE `{tableName}` CHANGE `{propName}` `{propName}` {getMySQLType(propType)} NOT NULL";
                        }

                        bool isPrimary = false;
                        if (propText.Contains("[Primary]")) //is primary key
                        {
                            isPrimary = true;
                            tableInfo[$"{objectName}.key"] = propName;
                            tableInfo[$"{objectName}.table"] = tableName;

                            primaryKey = $"PRIMARY KEY (`{propName}`)";
                            colSQL += $", ADD PRIMARY KEY (`{propName}`);";
                        }
                        if (propText.Contains("[Foreign")) //is foreign key
                        {
                            //extract parent table
                            //[Foreign(typeof(Category))] public int Fk { get; set;
                            string parentTable = new Kuto(propText).Extract("[Foreign(typeof(", ")").ToString().Trim();

                            indexes += $",INDEX (`{propName}`)";
                            colSQL += $", ADD INDEX (`{propName}`);";
                            RelationshipsQuery[parentTable] = $"`{ tableName }` ADD FOREIGN KEY (`{propName}`)";

                        }

                        sql += $"`{propName}` {getMySQLType(propType)} NOT NULL {(isPrimary ? "AUTO_INCREMENT" : "")},";
                        if (tableExists) Exec(colSQL);
                        columns.Add(propName.ToLower());
                    }

                    sql += primaryKey;
                    if (indexes.Length > 0) sql += indexes;
                    sql += ")";

                    if (isTableExists(tableName))
                    {
                        //delete extra columns
                        foreach (DataRow col in Query($"SHOW COLUMNS FROM `{tableName}`").Rows)
                        {
                            if (!columns.Contains(col[0].ToString().ToLower()))
                            {
                                //drop
                                Console.WriteLine($"Dropping {col[0].ToString().ToUpper()}");
                                Exec($"ALTER TABLE `{tableName}` DROP `{col[0].ToString().ToLower()}`");
                            }
                        }
                    }
                    else
                    {
                        Exec(sql);

                    }
                    Console.WriteLine();
                }

            }

            //drop extra tables
            foreach (DataRow table in Query($"SHOW TABLES").Rows)
            {
                if (!new_tables.Contains(table[0].ToString().ToLower()))
                {
                    //drop
                    Console.WriteLine($"Dropping {table[0].ToString().ToUpper()}");
                    Exec($"DROP TABLE `{table[0].ToString().ToLower()}");
                }
            }
            //remap relationships
            Console.WriteLine("Fixing Relationships");
            foreach (var relationship in RelationshipsQuery)
            {
                Console.WriteLine($"{relationship.Value} => {tableInfo[relationship.Key + ".table"]}".Replace("`", ""));
                var q = $"ALTER TABLE {relationship.Value} REFERENCES `{tableInfo[relationship.Key + ".table"]}`(`{tableInfo[relationship.Key + ".key"]}`) ON DELETE RESTRICT ON UPDATE RESTRICT;";
                Exec(q);

            }

            ///reganarate models
            Console.WriteLine("Fixing Model Files Formating.");
            System.Threading.Thread.Sleep(5000);
            GenerateModels(csprojProjectPath, nameSpace, ignoreList);

        }


        public string GenerateRelationships(string nameSpace = "Data.Models", string[] ignoreList = null)
        {
            StringBuilder txt = new StringBuilder();

            txt.AppendLine("using Micron;");
            txt.AppendLine("using System;");
            txt.AppendLine("using System.Collections.Generic;");
            txt.AppendLine("using System.Linq;");
            txt.AppendLine();
            txt.AppendLine("namespace " + nameSpace);
            txt.AppendLine("{");
            txt.AppendLine();

            foreach (DataRow table in Tables)
            {
                if (ignoreList.Contains(table[0].ToString().ToLower())) continue;

                txt.AppendLine($"#region {table[0].ToString().ToUpper()}");
                txt.AppendLine($" public partial class {PascalCase(ToSingular(table[0].ToString()))}");
                txt.AppendLine(" {");
                txt.AppendLine("public MicronDbContext DefaultDBContext { get; set; }");
                txt.AppendLine(GetOne(table[0].ToString(), ignoreList));
                txt.AppendLine(GetMany(table[0].ToString(), ignoreList));
                txt.AppendLine(" }");
                txt.AppendLine("#endregion");
            }
            txt.AppendLine();
            txt.AppendLine("}");

            return txt.ToString();
        }

        public string GenerateModel(string tableName, string nameSpace = "Data.Models", string[] ignoreList = null)
        {
            StringBuilder txt = new StringBuilder();

            txt.AppendLine("using Micron;");
            txt.AppendLine("using System;");
            txt.AppendLine("using System.Collections.Generic;");
            txt.AppendLine();
            txt.AppendLine("namespace " + nameSpace);
            txt.AppendLine("{");

            txt.AppendLine("/***" + ToSingular(tableName).ToUpper() + " MODEL***/");
            txt.AppendLine("  [Table(\"" + tableName + "\")]");
            txt.AppendLine(" public partial class " + PascalCase(ToSingular(tableName)) + " : IMicron");
            txt.AppendLine(" {");
            DataColumnCollection cols = this.Query("SELECT * FROM `" + tableName + "` LIMIT 0").Columns;
            string PrimaryKey = GetTablePK(tableName);
            foreach (DataColumn col in cols)
            {
                if (PrimaryKey.ToLower() == col.ColumnName.ToLower())
                {
                    txt.AppendLine("        [Primary]");
                }

                //check relationships
                try
                {
                    var relationships = Relationships.AsEnumerable()
                              .Where(r =>
                                  r.Field<string>("TABLE_NAME").ToLower() == tableName.ToLower()
                                  &&
                                  r.Field<string>("COLUMN_NAME").ToLower() == col.ColumnName.ToLower()

                              )?.CopyToDataTable();


                    if (relationships.Rows.Count > 0)
                    {
                        txt.AppendLine($"        [Foreign(typeof({PascalCase(ToSingular(relationships.Rows[0]["REFERENCED_TABLE_NAME"].ToString()))}))]");
                    }
                }
                catch (Exception)
                { }


                txt.AppendLine("        public " + col.DataType.Name + " " + col.ColumnName + " {get; set;}");
            }


            txt.AppendLine(" }");
            txt.AppendLine("}");

            Console.WriteLine();
            Console.Write(tableName + " ");
            ConsoleColor color = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Done");
            Console.BackgroundColor = color;
            return txt.ToString();
        }

        string GetOne(string tableName, string[] ignoreList)
        {
            try
            {
                StringBuilder txt = new StringBuilder();

                var relationships = Relationships.AsEnumerable()
                    .Where(r => r.Field<string>("TABLE_NAME") == tableName).CopyToDataTable();


                foreach (DataRow table in relationships.Rows)
                {
                    if (ignoreList.Contains(table[2].ToString().ToLower()))
                    {
                        txt.AppendLine("/**IGNORED " + table[2].ToString() + "**/");
                        continue;
                    }

                    if (!txt.ToString().Contains("Get" + PascalCase(ToSingular(table[2].ToString())) + "()"))
                    {
                        txt.AppendLine($"  public  {PascalCase(ToSingular(table[2].ToString()))} Get" + PascalCase(ToSingular(table[2].ToString())) + "() { return DefaultDBContext.GetRecord<" + PascalCase(ToSingular(table[2].ToString())) + ">(this." + table[1].ToString() + "); }");
                        txt.AppendLine("   public void Set" + PascalCase(ToSingular(table[2].ToString())) + "(" + PascalCase(ToSingular(table[2].ToString())) + " model)  {  DefaultDBContext.SetRelation(this, model);}");

                    }
                }

                return txt.ToString();
            }
            catch (Exception)
            {
                return "";
            }

        }

        string GetMany(string tableName, string[] ignoreList)
        {
            try
            {


                StringBuilder txt = new StringBuilder();

                var relationships = Relationships.AsEnumerable()
                    .Where(r => r.Field<string>("REFERENCED_TABLE_NAME") == tableName)?.CopyToDataTable();


                foreach (DataRow table in relationships?.Rows)
                {
                    if (ignoreList.Contains(table[0].ToString().ToLower()))
                    {
                        txt.AppendLine("/**IGNORED " + table[0].ToString() + "**/");
                        continue;
                    }

                    //detect if one to one
                    var pkData = Query($"SHOW KEYS FROM `{table[0].ToString()}` WHERE Key_name = 'PRIMARY'");

                    if (pkData.Rows.Count > 0)
                    {
                        if (pkData.Rows[0]["Column_name"].ToString().ToLower() == table[1].ToString().ToLower())
                        {
                            //then its one
                            if (!txt.ToString().Contains("Has" + PascalCase(table[0].ToString()) + "("))
                            {
                                txt.AppendLine($"   public bool Has" + PascalCase(ToSingular(table[0].ToString())) + "(string where=\"true\") {return DefaultDBContext.GetRecords<" + PascalCase(ToSingular(table[0].ToString())) + ">(\"" + table[1].ToString() + " = \"+this." + table[3].ToString() + "+\" AND \"+where+\" LIMIT 1\").Count()>0;}");
                                txt.AppendLine($"   public  {PascalCase(ToSingular(table[0].ToString()))}  Get" + ToSingular(PascalCase(table[0].ToString())) + "(string where=\"true\") {return DefaultDBContext.GetRecords<" + PascalCase(ToSingular(table[0].ToString())) + ">(\"" + table[1].ToString() + " = \"+this." + table[3].ToString() + "+\" AND \"+where).FirstOrDefault() ;}");
                                txt.AppendLine("    public void Set" + PascalCase(ToSingular(table[0].ToString())) + "(" + PascalCase(ToSingular(table[0].ToString())) + " model) { model.Set" + PascalCase(ToSingular(table[2].ToString())) + "(this); }");
                                continue;
                            }
                        }
                    }


                    if (!txt.ToString().Contains("Has" + PascalCase(table[0].ToString()) + "("))
                    {
                        txt.AppendLine($"   public bool Has" + PascalCase(table[0].ToString()) + "(string where=\"true\") {return DefaultDBContext.GetRecords<" + PascalCase(ToSingular(table[0].ToString())) + ">(\"" + table[1].ToString() + " = \"+this." + table[3].ToString() + "+\" AND \"+where+\" LIMIT 1\").Count()>0;}");
                        txt.AppendLine($"   public IEnumerable<{PascalCase(ToSingular(table[0].ToString()))}> Get" + PascalCase(table[0].ToString()) + "(string where=\"true\") {return DefaultDBContext.GetRecords<" + PascalCase(ToSingular(table[0].ToString())) + ">(\"" + table[1].ToString() + " = \"+this." + table[3].ToString() + "+\" AND \"+where);}");
                        txt.AppendLine("    public void Add" + PascalCase(ToSingular(table[0].ToString())) + "(" + PascalCase(ToSingular(table[0].ToString())) + " model) { model.Set" + PascalCase(ToSingular(table[2].ToString())) + "(this); }");
                        txt.AppendLine("    public void Add" + PascalCase(table[0].ToString()) + "(IEnumerable<" + PascalCase(ToSingular(table[0].ToString())) + "> models) {foreach(var model in models) model.Set" + PascalCase(ToSingular(table[2].ToString())) + "(this); }");
                    }
                }

                return txt.ToString();
            }
            catch (Exception)
            {
                return "";
            }



        }



        public string PascalCase(string word)
        {
            if (word.Trim().Contains("_"))
            {
                string[] words = word.Trim().Split('_');

                string ret = "";
                foreach (var item in words)
                {
                    ret += PascalCase(item) + "_";
                }
                return ret.Remove(ret.Length - 1);
            }

            return string.Join(" ", word.Split(' ')
                         .Select(w => w.Trim())
                         .Where(w => w.Length > 0)
                         .Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1).ToLower()));
        }



        string GetTablePK(string tableName)
        {
            DataRowCollection cols = this.Query("SHOW COLUMNS FROM `" + tableName + "`").Rows;
            string altCol = "";
            foreach (DataRow tableInfo in cols)
            {
                if (tableInfo[3].ToString() == "PRI")
                {
                    return tableInfo[0].ToString();
                }
                if (tableInfo[0].ToString().ToLower() == ToSingular(tableName).ToLower() + "id")
                {
                    altCol = tableInfo[0].ToString();
                }
            }

            if (altCol.Length > 0)
            {
                return altCol;
            }


            return cols[0].ToString();
        }

        public void BindDatagridViewEvents(DataGridView dataGridView, Type micronType, int primaryColumn = 0)
        {
            string TableName = GetTableName(micronType);
            dataGridView.CellEndEdit += (s, e) =>
            {
                this.CellEndEdit_Event(dataGridView, e, TableName, primaryColumn);
            };


            dataGridView.UserDeletingRow += (s, e) =>
            {

                this.UserDeletedRow_Event(dataGridView, e, TableName, primaryColumn);
            };



        }
        public bool UserDeletedRow_Event(object sender, DataGridViewRowCancelEventArgs e, string Table_name, int PKcolumn)
        {

            try
            {

                string id = ((DataGridView)sender).Columns[PKcolumn].HeaderText;

                string IDVAL = ((DataGridView)sender).Rows[e.Row.Index].Cells[PKcolumn].Value.ToString();

                string querry = "DELETE FROM `" + Table_name + "` WHERE `" + id + "` = " + IDVAL + " LIMIT 1;";

                this.Exec(querry);
                return true;

            }
            catch (Exception err)
            {
                setError(err);
                return false;
            }



        }
        public bool CellEndEdit_Event(object sender, DataGridViewCellEventArgs e, string Tablename, int PKcolumn)
        {

            try
            {
                after_edit = ((DataGridView)sender).Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();

            }
            catch (Exception)
            {
                after_edit = "";

            }
            try
            {


                if (before_edit != after_edit)
                {
                    string id = ((DataGridView)sender).Columns[PKcolumn].HeaderText;
                    string IDVAL = "";
                    try { IDVAL = ((DataGridView)sender).Rows[e.RowIndex].Cells[PKcolumn].Value.ToString(); }
                    catch (Exception) { }
                    string col = ((DataGridView)sender).Columns[e.ColumnIndex].HeaderText;
                    object ans = ((DataGridView)sender).Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                    if (ans.GetType() == typeof(string))
                    {
                        ans = "'" + ans.ToString().Replace("'", @"\'").Replace("\\", "\\\\'") + "'";
                    }

                    string querry = "";
                    if (IDVAL.Trim().Length > 0)
                    {
                        this.Exec("UPDATE `" + Tablename + "` SET `" + col + "` = " + ans + " WHERE `" + id + "` = '" + IDVAL + "' LIMIT 1;");

                    }
                    else
                    {
                        ////---if idval== balnk then its al new record
                        ////---GET THE NEXT AUTONUMBER

                        string thenewid = this.GetValue<string>("SELECT (MAX(" + id + ")+1) FROM  `" + Tablename + "`;").ToString();


                        if (thenewid.Trim().Length == 0)
                        {
                            thenewid = "0";
                        }

                        querry = "INSERT INTO `" + Tablename + "` (`" + col + "`,`" + id + "`) VALUES (" + ans + "," + thenewid + ");";

                        this.Exec(querry);
                        ((DataGridView)sender).Rows[e.RowIndex].Cells[PKcolumn].Value = thenewid;
                    }


                    return (true);

                }
                return false;
            }
            catch (Exception err)
            {
                setError(err);
                return false;

            }

        }


        public T GetValue<T>(string query, object columnNameOrIdx)
        {
            var a = Query(query);
            if (a.Rows.Count > 0)
            {
                if (isInt(columnNameOrIdx))
                {
                    return (T)Convert.ChangeType(a.Rows[0][(int)columnNameOrIdx], typeof(T));

                }
                return (T)Convert.ChangeType(a.Rows[0][columnNameOrIdx.ToString()], typeof(T));

            }
            return default(T);
        }
        public T GetValue<T>(string query)
        {
            return GetValue<T>(query, 0);
        }




        public object GetLastInsertID()
        {
            return this.lastInsertedID;
        }

        public int GetAffectedRecords()
        {
            return this.affectedRecords;
        }

        public string GetLastQuery()
        {
            return this.lastQuery;
        }




        public Exception GetLastException()
        {
            return this.lastException;
        }

        public void OpenConnection()
        {
            try
            {

                mysqlConnection.Open();
            }
            catch (Exception err)
            {
                setError(err);
            }
        }
        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void CloseConnection()
        {
            try
            {

                mysqlConnection.Close();
            }
            catch (Exception err)
            {
                setError(err);

            }
        }


        public IDbConnection GetMySQLConnection()
        {
            return this.mysqlConnection;
        }

        public void BeginTrasaction()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> GetColumn<T>(string queryOrTableName) where T : IMicron
        {

            string sql = queryOrTableName.Trim();
            if (!isSelectQuery(queryOrTableName))
            {
                sql = "SELECT * FROM `" + GetTableName(typeof(T)) + "`;";
            }

            return GetColumn<T>(sql, 0);
        }


        public IEnumerable<object> GetColumn<T>(string queryOrTableName, object columnNameOrIdx) where T : IMicron
        {
            string sql = queryOrTableName.Trim();
            if (!isSelectQuery(queryOrTableName))
            {
                sql = "SELECT * FROM `" + GetTableName(typeof(T)) + "`;";
            }

            var dt = Query(sql);
            if (isInt(columnNameOrIdx) && (int)columnNameOrIdx > dt.Columns.Count - 1)
            {
                return new string[] { };
            }
            if (dt.Rows.Count == 0)
            {
                return new string[] { };
            }

            List<object> a = new List<object>();
            foreach (DataRow item in dt.Rows)
            {
                if (isInt(columnNameOrIdx))
                {
                    a.Add(item[(int)columnNameOrIdx]);
                }
                else
                {
                    a.Add(item[columnNameOrIdx.ToString()]);

                }
            }
            return a;
        }




        bool IsNumber(object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }
        bool isInt(object value)
        {
            return value is int;
        }

        public string GetTableName(Type T)
        {

            foreach (var attribData in T.GetCustomAttributesData())
            {

                if (attribData.ToString().Contains("Micron.Table"))
                {
                    foreach (var item in attribData.ConstructorArguments)
                    {
                        return item.Value.ToString();
                    }
                }
            }

            string nameByClass = ToMany(T.Name);
            if (isTableExists(nameByClass))
            {
                return nameByClass;
            }

            string nameByPk = T.GetProperties()[0].Name;
            if (isTableExists(nameByPk))
            {
                return nameByPk;
            }

            throw new Exception("Table of type " + T.Name + " could not be found");
        }


        bool isTableExists(string tableName, bool reload = false)
        {
            try
            {
                if (reload) this.Tables = Query($"SHOW TABLES").Rows;

                for (int i = 0; i < this.Tables.Count; i++)
                {
                    if (tableName.ToLower().Trim() == this.Tables[i][0].ToString().ToLower().Trim())
                    {
                        return true;
                    }
                }
            }
            catch (Exception err)
            {

            }

            return false;
        }

        bool isColumnExists(string tableName, string colName)
        {

            try
            {
                var cols = Query($"SHOW COLUMNS FROM `{tableName}`").Rows;

                foreach (DataRow col in cols)
                {
                    if (col[0].ToString().ToLower() == colName.ToLower()) return true;
                }
            }
            catch (Exception)
            {

            }

            return false;
        }





        public string ToSingular(string word)
        {
            if (word.Trim().Contains("_"))
            {
                string[] words = word.Trim().Split('_');

                string ret = "";
                for (int i = 0; i < words.Length; i++)
                {

                    if (i < words.Length - 1)
                    {
                        ret += words[i] + "_";
                    }
                    else
                    {
                        ret += ToSingular(words[i]);
                    }
                }
                return ret;
            }

            PluralizationService ps = PluralizationService.CreateService(CultureInfo.GetCultureInfo(this.Locality));

            if (ps.IsPlural(word))
            {
                return ps.Singularize(word);
            }


            return word;
        }
        public string ToMany(string word)
        {
            if (word.Trim().Contains("_"))
            {
                string[] words = word.Trim().Split('_');

                string ret = "";
                for (int i = 0; i < words.Length; i++)
                {

                    if (i < words.Length - 1)
                    {
                        ret += words[i] + "_";
                    }
                    else
                    {
                        ret += ToMany(words[i]);
                    }
                }
                return ret;
            }

            PluralizationService ps = PluralizationService.CreateService(CultureInfo.GetCultureInfo(this.Locality));

            if (ps.IsSingular(word))
            {
                return ps.Pluralize(word);
            }

            return word;
        }



        public string GenerateInsert<T>(T modelObject, string tableName) where T : class, IMicron, new()
        {
            string cols = "(";
            string vals = "VALUES (";
            int i = 0;

            var pkProp = GetPrimaryKey(modelObject);

            foreach (PropertyInfo property in typeof(T).GetProperties())
            {

                if (property.PropertyType == typeof(MicronDbContext))
                {
                    continue;
                }


                if (property.GetValue(modelObject, null) != null && property.Name != pkProp.PrimaryKeyName)
                {
                    if (property.GetValue(modelObject, null).GetType() == typeof(DateTime))
                    {
                        cols += "`" + property.Name + "`,";
                        vals += "'" + DateTime.Parse(property.GetValue(modelObject, null).ToString()).ToString("yyyy-MM-dd HH:mm:ss") + "',";
                    }
                    if (property.GetValue(modelObject, null).GetType() == typeof(string))
                    {
                        cols += "`" + property.Name + "`,";
                        vals += "'" + property.GetValue(modelObject, null).ToString().Replace("'", @"\'").Replace("\\", "\\\\") + "',";
                    }
                    else
                    {
                        cols += "`" + property.Name + "`,";
                        vals += "" + property.GetValue(modelObject, null) + ",";
                    }

                }
                i++;
            }
            vals = vals.Substring(0, vals.Length - 1) + ")";
            cols = cols.Substring(0, cols.Length - 1) + ")";

            string str = "INSERT INTO `" + tableName + "` " + cols + " " + vals + ";";

            return str;
        }
        public string GenerateUpdate<T>(T modelObject, string tableName) where T : class, IMicron, new()
        {
            string str = "UPDATE `" + tableName + "` SET ";
            var pkProp = GetPrimaryKey(modelObject);
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {

                if (property.PropertyType == typeof(MicronDbContext))
                {
                    continue;
                }


                if (property.GetValue(modelObject, null) != null && property.Name != pkProp.PrimaryKeyName)
                {
                    if (property.GetValue(modelObject, null).GetType() == typeof(DateTime))
                    {
                        str += "`" + property.Name + "`='" + DateTime.Parse(property.GetValue(modelObject, null).ToString()).ToString("yyyy-MM-dd HH:mm:ss") + "',";
                    }
                    if (property.GetValue(modelObject, null).GetType() == typeof(string))
                    {
                        str += "`" + property.Name + "`='" + property.GetValue(modelObject, null).ToString().Replace("'", @"\'").Replace("\\", "\\\\") + "',";
                    }
                    else
                    {
                        str += "`" + property.Name + "`=" + property.GetValue(modelObject, null) + ",";
                    }

                }
            }
            str = str.Substring(0, str.Length - 1) + " WHERE `" + pkProp.PrimaryKeyName + "` = '" + pkProp.PrimaryKeyValue + "';";
            return str;
        }

        public string GenerateDelete<T>(T modelObject, string tableName) where T : class, IMicron, new()
        {
            try
            {
                var pkProp = GetPrimaryKey(modelObject);
                return "DELETE FROM `" + tableName + "` WHERE `" + pkProp.PrimaryKeyName + "` = '" + pkProp.PrimaryKeyValue + "';";

            }
            catch (Exception)
            {
                return null;
            }
        }
        public PrimaryKeyProperties GetPrimaryKey(IMicron modelObject)
        {
            PrimaryKeyProperties r = null;
            foreach (PropertyInfo property in modelObject.GetType().GetProperties())
            {
                if (r == null)
                {   //set first col
                    r = new PrimaryKeyProperties
                    {
                        PrimaryKeyName = property.Name,
                        PrimaryKeyValue = property.GetValue(modelObject, null)
                    };

                }
                //detect the Primary attribute
                foreach (var attrib in property.GetCustomAttributes(false))
                {
                    if (attrib.GetType() == typeof(PrimaryAttribute))
                    {
                        return new PrimaryKeyProperties()
                        {
                            PrimaryKeyName = property.Name,
                            PrimaryKeyValue = property.GetValue(modelObject, null)
                        };
                    }
                }
            }

            return r;
        }

        public PrimaryKeyProperties GetPrimaryKey(Type modelType)
        {
            PrimaryKeyProperties r = null;
            foreach (PropertyInfo property in modelType.GetProperties())
            {
                if (r == null)
                {   //set first col
                    r = new PrimaryKeyProperties
                    {
                        PrimaryKeyName = property.Name,
                        PrimaryKeyValue = null
                    };

                }
                //detect the Primary attribute
                foreach (var attrib in property.GetCustomAttributes(false))
                {
                    if (attrib.GetType() == typeof(PrimaryAttribute))
                    {
                        return new PrimaryKeyProperties()
                        {
                            PrimaryKeyName = property.Name,
                            PrimaryKeyValue = null
                        };
                    }
                }
            }

            return r;

        }

        public void SetRelation(IMicron model, IMicron model2)
        {
            string table1 = GetTableName(model.GetType());
            string table2 = GetTableName(model2.GetType());

            var pki1 = GetPrimaryKey(model);
            var pki2 = GetPrimaryKey(model2);

            var rlb = Relationships.AsEnumerable()
                    .Where(r => r.Field<string>("TABLE_NAME").ToLower() == table1.ToLower()
                    && r.Field<string>("REFERENCED_TABLE_NAME").ToLower() == table2.ToLower()
                    ).CopyToDataTable();
            Exec($"UPDATE `{table1}` SET `{rlb.Rows[0]["COLUMN_NAME"]}`='{pki2.PrimaryKeyValue}' WHERE `{pki1.PrimaryKeyName}`='{pki1.PrimaryKeyValue}'");
        }



    }

    public class PrimaryKeyProperties
    {
        public object PrimaryKeyValue { get; internal set; }
        public string PrimaryKeyName { get; internal set; }
    }
}
