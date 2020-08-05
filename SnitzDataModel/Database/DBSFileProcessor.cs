// /*
// ####################################################################################################################
// ##
// ## DBSFileProcessor
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using Snitz.Base;
using SnitzConfig;
using SnitzDataModel.Database;

namespace SnitzDataModel
{
    public class DbsFileProcessor
    {
        private static object syncRoot = new object();
        private static IDictionary<string, string> ProcessStatus { get; set; }

        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
        public bool Applied { get; set; }
        public string AdminUser { get; set; }
        public string AdminPassword { get; set; }
        public string AdminEmail { get; set; }
        public HttpContextBase Context { get; set; }

        public bool Success;
        private XDocument dbsDocument;
        private string _filename = string.Empty;
        public string _dbType = "mssql";

        //public StringBuilder Errors;
        SnitzDataContext dal;

        public DbsFileProcessor(string filename)
        {
            if (!File.Exists(filename))
            {
                Success = false;
                return;
            }
            if (ProcessStatus == null)
            {
                ProcessStatus = new Dictionary<string, string>();
            }
            dal = new SnitzDataContext();
            _filename = filename;
            dbsDocument = XDocument.Load(filename);

            XElement root = dbsDocument.Element("Tables");
            if (root != null) Applied = Convert.ToBoolean(root.Attribute("applied").Value);
            Success = false;
            HasErrors = false;
        }

        public DbsFileProcessor()
        {
            HasErrors = false;
            _dbType = "mssql";
            if (ProcessStatus == null)
            {
                ProcessStatus = new Dictionary<string, string>();
            }
        }

        public string Process(string id)
        {
            if (Applied)
            {
                throw new Exception("File already processed");
            }

            XElement root = dbsDocument.Element("Tables");

            try
            {
                //Errors = new StringBuilder();
                if (root != null)
                {
                    XElement createtables = root.Element("Create");
                    if (createtables != null)
                    {
                        Log(id, "Creating Tables");
                        CreateTables(createtables, id);
                        Log(id, "--------------------------------");
                    }
                    XElement altertables = root.Element("Alter");
                    if (altertables != null)
                    {
                        Log(id, "Alter Tables");
                        AlterTables(altertables, id);
                        Log(id, "--------------------------------");
                    }
                    XElement updatetables = root.Element("Update");
                    if (updatetables != null)
                    {
                        Log(id, "Updating Tables");
                        TableUpdates(updatetables, id);
                        Log(id, "--------------------------------");
                    }
                    XElement inserttables = root.Element("Insert");
                    if (inserttables != null)
                    {
                        Log(id, "Inserting Data into Tables");
                        TableInserts(inserttables, id);
                        Log(id, "--------------------------------");
                    }
                    XElement deletetables = root.Element("Delete");
                    if (deletetables != null)
                    {
                        Log(id, "Deleting from Tables");
                        TableDeletes(deletetables, id);
                        Log(id, "--------------------------------");
                    }
                    XElement droptables = root.Element("Drop");
                    if (droptables != null)
                    {
                        Log(id, "Dropping Tables");
                        DropTables(droptables, id);
                        Log(id, "--------------------------------");
                    }
                    XElement indices = root.Element("Indexing");
                    if (indices != null)
                    {
                        Log(id, "Creating Indices");
                        CreateIndices(indices, id);
                        Log(id, "--------------------------------");
                    }
                }
                root.SetAttributeValue("applied", true);
                try
                {
                    dbsDocument.Save(_filename);
                }
                catch (Exception e)
                {
                    Log(id, "WARNING:" + e.Message);
                }

                // add updatecount stored procedure
                if (Config.RunSetup)
                {
                    try
                    {
                        Log(id, "Adding stored procedures");

                        string appDataPath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
                        ExecuteSQLScript(_dbType == "mysql"
                            ? Path.Combine(appDataPath, "updatecountSPMySql.sql")
                            : Path.Combine(appDataPath, "updatecountSP.sql"));
                        Thread.Sleep(100);
                        Log(id, "Adding language strings");
                        try
                        {
                            var langfiles = Directory.GetFiles(appDataPath, "language_*.csv");
                            foreach (string langfile in langfiles)
                            {
                                SnitzDataContext.ImportLangResCSV(Path.Combine(appDataPath, "language_en.csv"), true);
                            }

                        }
                        catch (Exception)
                        {
                            Log(id, "WARNING: problem importing language file language_en.csv");
                        }

                    }
                    catch (Exception e)
                    {
                        Log(id, "ERROR: runningSQLScripts" + e.Message);
                    }
                    if (_filename.Contains("base.xml"))
                    {
                        InitDatabase(id);
                    }
                }
                Success = true;
            }
            catch (Exception ex)
            {
                Success = false;
                Log(id, "ERROR:" + ex.Message);
            }

            Thread.Sleep(200);

            Log(id, "Complete");
            Thread.Sleep(200);
            return id;
        }

        private void Log(string id, string logdata)
        {
            if (logdata.StartsWith("ERROR", StringComparison.Ordinal))
            {
                logdata = "<span style='color:red;'>" + logdata + "</span><br />";
                HasErrors = true;
            }
            else if (logdata.StartsWith("WARNING", StringComparison.Ordinal))
            {
                logdata = "<span style='color:orange;'>" + logdata + "</span><br />";
                HasWarnings = true;
            }
            else
            {
                logdata = logdata + "<br />";
            }
            ProcessStatus[id] = ProcessStatus[id] + logdata;

        }

        public void Add(string id)
        {
            lock (syncRoot)
            {
                ProcessStatus.Add(id, "Process Added");
            }
        }

        /// <summary>
        /// Removes the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Remove(string id)
        {
            lock (syncRoot)
            {
                ProcessStatus.Remove(id);
            }
        }
        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <param name="id">The id.</param>
        public string GetStatus(string id)
        {
            lock (syncRoot)
            {
                if (ProcessStatus.Keys.Count(x => x == id) == 1)
                {
                    var status = ProcessStatus[id];
                    ProcessStatus[id] = "";
                    return status;
                }

                if (!HasErrors && HasWarnings)
                {
                    return "Complete With warnings";
                }
                return "Complete" + (HasErrors ? " With errors" : "");
            }
        }


        public void ExecuteSQLScript(string filename)
        {
            if (File.Exists(filename))
            {
                string filecontent = File.ReadAllText(filename);
                dal.Execute(filecontent);
            }
        }

        private void InitDatabase(string id)
        {
            Log(id, "Initialise Database");
            Models.Member admin = new Models.Member
            {
                Username = this.AdminUser,
                UserLevel = 3,
                CreatedDate = DateTime.UtcNow,
                LastVisitDate = DateTime.UtcNow,
                Email = this.AdminEmail,
                PostCount = 1,
                IsValid = 1
            };
            admin.SnitzPassword = SHA256Hash(this.AdminPassword);
            admin.Save();

            Log(id, "Created Admin user");

            Models.Category cat = new Models.Category { Title = "Snitz Forums Mvc", Status = Enumerators.Status.Open, Order = 0 };
            cat.Save();
            Log(id, "Created default category");

            Models.Forum forum = new Models.Forum
            {
                CatId = cat.Id,
                Subject = "Testing Forums",
                LastPostDate = DateTime.UtcNow,
                TopicCount = 1,
                LastPostAuthorId = admin.Id,
                LastPostReplyId = 0,
                AllowRating = 0,
                Description =
                        "This forum gives you a chance to become more familiar with how this product responds to different features and keeps testing in one place instead of posting tests all over. Happy Posting! [:)]"
            };
            forum.Save();
            Log(id, "Created default Forum");

            Models.Topic topic = new Models.Topic
            {
                ForumId = forum.Id,
                CatId = cat.Id,
                DoNotArchive = 0,
                Subject = "Welcome to Snitz Forums Mvc",
                Message =
                                  "Thank you for downloading the Snitz Forums Mvc. We hope you enjoy this great tool to support your organization! [br][br]Many thanks go out to John Penfold &lt;asp@asp-dev.com&gt; and Tim Teal &lt;tteal@tealnet.com&gt; for the original source code and to all the people of Snitz Forums 2000 at http://forum.snitz.com for continued support of this product.",
                Date = DateTime.UtcNow,
                AuthorId = admin.Id,
                PosterIp = "0.0.0.0",
                ShowSig = 0,
                LastPostAuthorId = admin.Id,
                LastPostReplyId = 0,
                PostStatus = Enumerators.PostStatus.Closed,
                LastPostDate = DateTime.UtcNow
            };
            topic.Save();
            forum.LastPostTopicId = topic.Id;
            forum.Save();
            Log(id, "Created default Topic");
            dal.UpdatePostCount();

        }

        private void CreateIndices(XElement indices, string id)
        {

            IEnumerable<XElement> tables = indices.Elements("Table");
            foreach (var table in tables)
            {
                var xTable = table.Attribute("name");
                foreach (var index in table.Elements("Index"))
                {
                    //<Index name="PK_FORUM_CATEGORY" columns="CAT_ID,COL_2" direction="ASC" unique="true"/>
                    //CREATE [ UNIQUE ] INDEX index ON table (field [ASC|DESC][, field [ASC|DESC], ]) [WITH { PRIMARY | DISALLOW NULL | IGNORE NULL }]
                    StringBuilder sql = new StringBuilder();
    string unique = "";
                    if (index.Attribute("unique") != null)
                        unique = "UNIQUE";
                    sql.AppendFormat("CREATE {0} INDEX {1} ON {2} (", unique, index.Attribute("name").Value,
                        xTable.Value);
                    var columns = index.Attribute("columns").Value.Split(',');
    bool first = true;
                    foreach (string column in columns)
                    {
                        if (!first) sql.Append(", ");
                        sql.AppendFormat("{0} {1}", column, index.Attribute("direction").Value);
                        first = false;
                    }
sql.AppendLine(")");
                    try
                    {
                        Log(id, "CreateIndices: " + sql);
dal.Execute(sql.ToString());
                    }
                    catch(Exception ex)
                    {
                        Log(id, "ERROR: " + "CreateIndices: " + ex.Message);
                    }
                }
            }
        }

        private void DropTables(XElement droptables, string id)
{

    IEnumerable<XElement> tables = droptables.Elements("Table");
    foreach (var table in tables)
    {
        var xTable = table.Attribute("name");
        StringBuilder sql = new StringBuilder();
        if (xTable != null) sql.AppendFormat("DROP TABLE {0}", xTable.Value);
        try
        {
            Log(id, "DropTable: " + sql);
            dal.Execute(sql.ToString());
        }
        catch (Exception ex)
        {
            Log(id, "ERROR: " + "DropTables: " + ex.Message);
        }
    }
}

private void TableDeletes(XElement deletetables, string id)
{
    IEnumerable<XElement> tables = deletetables.Elements("Table");
    foreach (var table in tables)
    {
        var xTable = table.Attribute("name");
        var xWhere = table.Attribute("condition");
        StringBuilder sql = new StringBuilder();
        if (xTable != null)
            if (xWhere != null) sql.AppendFormat("DELETE FROM {0} WHERE {1}", xTable.Value, xWhere.Value);
        try
        {
            Log(id, "TableDeletes: " + xTable.Value);
            dal.Execute(sql.ToString());
        }
        catch (Exception ex)
        {
            Log(id, "ERROR: " + "TableDeletes: " + ex.Message);
        }
    }
}

private void TableInserts(XElement inserttables, string id)
{
    IEnumerable<XElement> tables = inserttables.Elements("Table");
    foreach (var table in tables)
    {
        bool first = true;
        var xTable = table.Attribute("name");
        StringBuilder cols = new StringBuilder();
        StringBuilder vals = new StringBuilder();
        foreach (var column in table.Elements("Column"))
        {
            var xColumn = column.Attribute("name");
            var xValue = column.Attribute("value");
            var xType = column.Attribute("type");
            if (!first)
            {
                cols.Append(",");
                vals.Append(",");
            }
            if (xColumn != null) cols.Append(xColumn.Value);
            if (xType != null && (xType.Value == "varchar" || xType.Value == "memo" || xType.Value == "nvarchar"))
                vals.Append("'");
            if (xValue != null) vals.Append(xValue.Value);
            if (xType != null && (xType.Value == "varchar" || xType.Value == "memo" || xType.Value == "nvarchar"))
                vals.Append("'");
            first = false;
        }
        //IF EXISTS(SELECT* FROM Bookings WHERE FLightID = @Id)
        //BEGIN
        //--UPDATE HERE
        //END
        //ELSE
        //BEGIN
        //-- INSERT HERE
        //END
        StringBuilder sql = new StringBuilder("INSERT INTO " + xTable.Value).AppendLine();

        if (xTable.Value.ToUpper() == dal.ForumTablePrefix + "CONFIG_NEW")
        {
            //check it doesn't exist first
            var col = cols.ToString().Split(',');
            int x = 0;
            foreach (var c in col)
            {
                if (c == "C_VARIABLE")
                    break;
                x += 1;
            }
            var val = vals.ToString().Split(',');
            if (!ClassicConfig.KeyExists(val[x].Replace("'", "")))
            {
                sql.AppendFormat("({0}) VALUES ({1})", cols, vals);
            }
        }
        else
        {
            sql.AppendFormat("({0}) VALUES ({1})", cols, vals);
        }

        try
        {
            Log(id, "Table Insert: " + sql.ToString());
            dal.Execute(sql.ToString());
        }
        catch (Exception ex)
        {
            Log(id, "ERROR: " + "TableInserts: " + ex.Message);
        }
    }
}

private void TableUpdates(XElement updatetables, string id)
{
    IEnumerable<XElement> tables = updatetables.Elements("Table");
    foreach (var table in tables)
    {
        var xWhere = table.Attribute("condition");
        var xTable = table.Attribute("name");

        StringBuilder sql = new StringBuilder("UPDATE " + xTable.Value + " SET ").AppendLine();
        bool first = true;
        foreach (XElement column in table.Elements("Column"))
        {
            var xColumn = column.Attribute("name");
            var xValue = column.Attribute("value");
            var xType = column.Attribute("type");
            //SqlDbType type = (SqlDbType)Enum.Parse(typeof(SqlDbType), xType.Value, true);

            if (!first)
                sql.Append(",");
            if (xColumn != null) sql.Append(xColumn.Value).Append("=");
            if (xType != null && (xType.Value == "varchar" || xType.Value == "memo" || xType.Value == "nvarchar"))
                sql.Append("'");
            if (xValue != null) sql.Append(xValue.Value);
            if (xType != null && (xType.Value == "varchar" || xType.Value == "memo" || xType.Value == "nvarchar"))
                sql.Append("'");
            sql.AppendLine();

            first = false;
        }
        if (xWhere != null && !String.IsNullOrEmpty(xWhere.Value))
            sql.AppendLine(xWhere.Value);
        try
        {
            Log(id, "UpdateTable: " + sql);
            dal.Execute(sql.ToString());
        }
        catch (Exception ex)
        {
            Log(id, "ERROR: " + "UpdateTables: " + ex.Message);
        }
    }
}

/// <summary>
/// AlterTables
/// </summary>
/// <param name="altertables"></param>
/// <param name="id"></param>
private void AlterTables(XElement altertables, string id)
{
    StringBuilder sql = new StringBuilder();
    IEnumerable<XElement> tables = altertables.Elements("Table");
    foreach (var table in tables)
    {
        //Create table sql
        var xTable = table.Attribute("name");
        var autoid = table.Attribute("idfield");
        //if (autoid != null)
        //{
        //    switch (_dbType)
        //    {
        //        case "access":
        //            sql = sql.AppendLine(autoid.Value + " COUNTER CONSTRAINT PrimaryKey PRIMARY KEY,");
        //            break;
        //        case "mssql":
        //            sql = sql.AppendLine(autoid.Value + " int IDENTITY (1, 1) PRIMARY KEY NOT NULL,");
        //            break;
        //        case "mysql":
        //            sql = sql.AppendLine(autoid.Value + " INT (11) NOT NULL auto_increment,");
        //            break;
        //    }

        //}
        if (xTable != null)
        {
            foreach (var column in table.Elements("Column"))
            {
                sql.Length = 0;
                var xColumn = column.Attribute("name");
                var action = column.Attribute("action");
                if (_dbType == "mysql" && action.Value == "ALTER")
                {
                    action.Value = "CHANGE";
                }
                if (xColumn != null)
                {
                    //IF COL_LENGTH('schemaName.tableName', 'columnName') IS NULL
                    //if NOT exists(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE Column_Name = xColumn.Value AND Table_Name = xTable.Value

                    sql.AppendFormat("ALTER TABLE {0} {1} ", xTable.Value, action.Value);
                    if (_dbType == "access" || action.Value != "ADD")
                    {
                        sql.Append(" COLUMN ");
                    }
                    if (_dbType == "mysql" && action.Value == "CHANGE")
                    {
                        sql.AppendFormat("{0} {1} {2} {3} {4} {5}",
                            xColumn.Value, xColumn.Value,
                            ColumnType(column.Attribute("type").Value),
                            ColumnSize(column.Attribute("size"), column.Attribute("type").Value, _dbType),
                            DefaultVal(column.Attribute("default"), column.Attribute("type").Value),
                            ColumnNull(column.Attribute("allownulls").Value, _dbType));
                    }
                    else if (action.Value == "ALTER")
                    {
                        sql.AppendFormat("{0} {1} {2} {3}",
                            xColumn.Value,
                            ColumnType(column.Attribute("type").Value),
                            ColumnSize(column.Attribute("size"), column.Attribute("type").Value, _dbType),
                            ColumnNull(column.Attribute("allownulls").Value, _dbType)
                            );
                        if (column.Attribute("default") != null)
                        {
                            sql.AppendFormat("; ALTER TABLE {0} ADD CONSTRAINT DF_{1} {2} FOR {3}", xTable.Value, xColumn.Value, DefaultVal(column.Attribute("default"), column.Attribute("type").Value), xColumn.Value);
                        }
                        //ALTER TABLE {0} ADD CONSTRAINT DF_SomeName {1} FOR {2};
                    }
                    else
                    {
                        sql.AppendFormat("{0} {1} {2} {3} {4}",
                        xColumn.Value,
                        ColumnType(column.Attribute("type").Value),
                        ColumnSize(column.Attribute("size"), column.Attribute("type").Value, _dbType),
                        ColumnNull(column.Attribute("allownulls").Value, _dbType),
                        DefaultVal(column.Attribute("default"), column.Attribute("type").Value));
                    }
                    //END
                    try
                    {
                        Log(id, "AlterTable: " + xTable.Value);
                        dal.OneTimeCommandTimeout = 300;
                        dal.Execute(sql.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log(id, "ERROR: " + "AlterTables: " + ex.Message);
                    }

                }
            }
        }
    }
}

private void CreateTables(XElement createtables, string id)
{
    IEnumerable<XElement> tables = createtables.Elements("Table");
    foreach (var table in tables)
    {
        var xTable = table.Attribute("name");
        var dropfirst = table.Attribute("droprename");
        string sqlDrop = "";

        if (xTable != null)
        {
            if (dropfirst != null)
            {
                if (dropfirst.Value == "rename")
                {
                    sqlDrop =
                        "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[" + xTable.Value + "]') AND type in (N'U')) " +
                        "EXEC sp_rename '" + xTable.Value + "', '" + xTable.Value + "_BAK';";
                    if (_dbType == "mysql")
                    {
                        sqlDrop = "RENAME TABLE " + xTable.Value + " TO " + xTable.Value + "_BAK;";
                    }
                }
                else
                {
                    sqlDrop =
                        "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[" + xTable.Value + "]') AND type in (N'U')) " +
                        "DROP TABLE [" + xTable.Value + "];";
                    if (_dbType == "mysql")
                    {
                        sqlDrop = "DROP TABLE [" + xTable.Value + "] IF EXISTS;";
                    }
                    dropfirst.Value = "drop";
                }

                //

            }
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("");
            sql.Append("CREATE TABLE " + xTable.Value + " (");
            var idcolumn = table.Attribute("idfield");
            if (idcolumn != null)
            {
                switch (_dbType)
                {
                    case "access":
                        sql = sql.AppendLine(idcolumn.Value + " COUNTER CONSTRAINT PrimaryKey PRIMARY KEY,");
                        break;
                    case "mssql":
                        sql = sql.AppendLine(idcolumn.Value + " int IDENTITY (1, 1) PRIMARY KEY NOT NULL,");
                        break;
                    case "mysql":
                        sql = sql.AppendLine(idcolumn.Value + " INT (11) NOT NULL auto_increment,");
                        break;
                }

            }
            bool first = true;
            foreach (var column in table.Elements("Column"))
            {
                if (!first) sql.AppendLine(", ");
                sql.AppendFormat("{0} {1} {2} {3} {4}", column.Attribute("name").Value,
                    ColumnType(column.Attribute("type").Value),
                    ColumnSize(column.Attribute("size"), column.Attribute("type").Value, _dbType),
                    ColumnNull(column.Attribute("allownulls").Value, _dbType),
                    DefaultVal(column.Attribute("default"), column.Attribute("type").Value));
                first = false;
            }

            if (_dbType == "mysql" && idcolumn != null)
            {
                sql.AppendFormat(",KEY {0}_{1} ({1})", xTable.Value,
                    idcolumn.Value);
            }
            sql.AppendLine(")");
            try
            {
                if (!String.IsNullOrWhiteSpace(sqlDrop))
                {
                    try
                    {
                        dal.Execute(sqlDrop);
                    }
                    catch (Exception e)
                    {
                        Log(id, "ERROR: " + dropfirst.Value + ": " + e.Message);
                    }
                }
                Log(id, "CreateTable: " + xTable.Value);
                dal.Execute(sql.ToString());
            }
            catch (Exception ex)
            {
                Log(id, "ERROR: " + "CreateTable: " + ex.Message);
                Log(id, "ERROR: " + "CreateTable: " + sql.ToString());
            }
            //create indexes
            sql.Length = 0;
            if (table.Elements("Index").Any())
            {
                foreach (var index in table.Elements("Index"))
                {

                    string unique = "";
                    if (index.Attribute("unique") != null)
                        unique = "UNIQUE";
                    sql.AppendFormat("CREATE {0} INDEX {1} ON {2} (", unique, index.Attribute("name").Value,
                        xTable.Value);
                    var columns = index.Attribute("columns").Value.Split(',');
                    first = true;
                    foreach (string column in columns)
                    {
                        if (!first) sql.Append(", ");
                        sql.AppendFormat("{0} {1}", column, index.Attribute("direction").Value);
                        first = false;
                    }
                    sql.AppendLine(");");
                }
                try
                {

                    dal.Execute(sql.ToString());
                }
                catch (Exception ex)
                {
                    Log(id, "ERROR: " + "Index: " + ex.Message);

                }
            }
        }
    }
}

public static string ColumnSize(XAttribute size, string type, string dbType)
{
    if (size != null)
    {
        switch (type)
        {
            case "smallint":
                if (dbType == "mysql")
                {
                    return "(" + size.Value + ")";
                }
                return "";
            case "int":
                if (dbType == "mysql")
                {
                    return "(" + size.Value + ")";
                }
                return "";
            case "nvarchar":
            case "varchar":
                return "(" + size.Value + ")";
            case "date":
                return "";
            default:
                return "";
        }

    }

    return "";
}

public static string DefaultVal(XAttribute value, string type)
{
    if (value == null) return "";

    if (!String.IsNullOrEmpty(value.Value))
    {
        switch (type)
        {
            case "smallint":
            case "bit":
            case "int":
                return "DEFAULT " + value.Value;
            case "float":
                return "DEFAULT '" + value.Value + "'";
            case "nvarchar":
            case "varchar":
                return "DEFAULT '" + value.Value + "'";
            case "date":
                return "DEFAULT '" + value.Value + "'";
            default:
                return value.Value;
        }

    }
    return "";
}

public static String ColumnNull(string value, string dbType)
{
    if (String.IsNullOrEmpty(value) || value == "true")
    {
        if (dbType == "mysql")
            return "";
        return "NULL";
    }
    return "NOT NULL";
}

private string ColumnType(string value)
{
    switch (_dbType)
    {
        case "access":
            switch (value)
            {
                case "smallint":
                    return value;
                case "int":
                    return value;
                case "nvarchar":
                case "varchar":
                    return "text";
                case "memo":
                    return value;
                case "date":
                    return value;
                default:
                    return value;
            }
        case "mssql":
            switch (value)
            {
                case "smallint":
                    return value;
                case "int":
                    return value;
                case "nvarchar":
                case "varchar":
                    return value;
                case "memo":
                case "text":
                    return "nvarchar(MAX)";
                case "date":
                    return "datetime";
                case "guid":
                    return "uniqueidentifier";
                default:
                    return value;
            }
        case "mysql":
            switch (value)
            {
                case "bit":
                    return "tinyint(1)";
                case "smallint":
                    return value;
                case "smallmoney":
                    return "decimal(5,2)";
                case "int":
                    return value;
                case "nvarchar":
                case "varchar":
                    return value;
                case "memo":
                case "text":
                    return "text";
                case "date":
                    return "datetime";
                case "guid":
                    return "uniqueidentifier";
                default:
                    return value;
            }
    }
    return String.Empty;
}

private string SHA256Hash(string password)
{
    SHA256 sha = new SHA256Managed();
    byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(password));

    var stringBuilder = new StringBuilder();
    foreach (byte b in hash)
    {
        stringBuilder.AppendFormat("{0:x2}", b);
    }
    return stringBuilder.ToString();
}    
    }
}
