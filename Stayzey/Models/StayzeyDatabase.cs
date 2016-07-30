using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Stayzey.Models
{
    public class StayzeyDatabase
    {
        public SqlConnection _connection = null;

        public StayzeyDatabase()
        {
            
        }

        public SqlConnection connection
        {
            get
            {
                if (_connection == null)
                {
                    string connstr = System.Configuration.ConfigurationManager.ConnectionStrings["StayzeyDbContext"].ConnectionString;
                    SqlConnection conn = new SqlConnection(connstr);
                    conn.Open();
                    _connection = conn;
                }

                return _connection;
            }
        }

        public void Close()
        {
            if (_connection != null) _connection.Close();
        }

        public List<Hashtable> Query(string sql)
        {
            List<Hashtable> data = new List<Hashtable>();
            
            SqlCommand cmd = new SqlCommand(sql, connection);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Hashtable row = new Hashtable();
                for(int i = 0; i < reader.FieldCount; i++)
                {
                    string fieldName = reader.GetName(i);
                    row[fieldName] = reader[fieldName];
                }
                data.Add(row);
            }
            reader.Close();
            return data;
        }

        public List<Hashtable> Query(string sql,SqlParameter[] parameters)
        {
            List<Hashtable> data = new List<Hashtable>();

            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddRange(parameters);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Hashtable row = new Hashtable();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string fieldName = reader.GetName(i);
                    row[fieldName] = reader[fieldName];
                }
                data.Add(row);
            }
            reader.Close();
            return data;
        }

        public int Update(string sql)
        {
            SqlCommand cmd = new SqlCommand(sql, connection);
            return cmd.ExecuteNonQuery();
        }

        public int Update(string sql,SqlParameter[] parameters)
        {
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public int Update(string tablename,Hashtable item,string idField)
        {
            string [] keys = new string[item.Keys.Count];
            item.Keys.CopyTo(keys, 0);
            SqlParameter[] parameters = new SqlParameter[keys.Count()];
            int i = 0;
            foreach(string key in keys)
            {
                if(key!=idField)
                {
                    parameters[i++] = new SqlParameter(key,item[key]);
                }
            }
            parameters[i++] = new SqlParameter(idField, item[idField]);

            string sql = "update "+tablename+" set ";
            foreach (string key in keys)
            {
                if (key != idField)
                {
                    sql += ""+key+"=@"+key+",";
                }
            }
            sql = sql.Substring(0, sql.Length - 1);
            sql += " where " + idField + "=@" + idField;

            return Update(sql, parameters);
        }

        public int Insert(string tablename,Hashtable item)
        {
            string[] keys = new string[item.Keys.Count];
            item.Keys.CopyTo(keys, 0);
            SqlParameter[] parameters = new SqlParameter[keys.Count()];
            int i = 0;
            foreach(string key in keys)
            {
                parameters[i++] = new SqlParameter(key,item[key]);
            }

            string sql = "insert into " + tablename + " (";

            foreach(string key in keys)
            {
                sql += key + ",";
            }
            sql = sql.Substring(0, sql.Length - 1);
            sql += ") values (";

            foreach(string key in keys)
            {
                sql += "@" + key + ",";
            }
            sql = sql.Substring(0, sql.Length - 1);
            sql += ") ";

            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public int GetLastInsertId()
        {
            List<Hashtable> result = Query("select @@IDENTITY as LastInsertId");
            if(result.Count>0)
            {
                string strId = ""+(result[0]["LastInsertId"]);
                int id = 0;
                int.TryParse(strId, out id);
                return id;
            }
            return 0;
        }
        
    }
}