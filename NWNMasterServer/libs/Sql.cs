using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace NWNMasterServer.libs
{
    class Sql
    {
        // Tabella accounts
        public const String TABLE_ACCOUNTS = "accounts";

        /**
         * Costruttore 
         */
        public Sql()
        {
        }


        /**
         * Autentica account dati username, password hash e salt 
         */
        public bool DoAuthUsername(String username, String pwhash, byte[] salt, out String error)
        {
            // 
            bool auth = false;
            // Preimposta error
            error = "";

            try
            {
                // Odbc
                OdbcConnection odbc = new OdbcConnection("DSN=nwn_master_server");
                odbc.Open();

                using (OdbcCommand dbcmd = odbc.CreateCommand())
                {
                    // Query selezione account
                    String query = "SELECT * FROM " + TABLE_ACCOUNTS + " WHERE username='" + username + "'";
                    dbcmd.CommandText = query;

                    using (OdbcDataReader dbreader = dbcmd.ExecuteReader())
                    {
                        while (dbreader.Read())
                        {
                            if (pwhash == Hashing.Md5PwdHashStr((String)dbreader["password"], salt))
                                auth = true;
                        }

                        dbreader.Close();
                    }
                }

                odbc.Close();
            }
            catch (Exception e)
            {
                error = e.StackTrace;
            }

            // Exit
            return auth;
        }

        // Check esistenza account
        public bool GetAccountExists(String username, out String error) {
            bool exists = false;
            error = "";

            try
            {
                // Connesione db
                OdbcConnection odbc = new OdbcConnection("DSN=nwn_master_server");
                odbc.Open();

                using (OdbcCommand dbcmd = odbc.CreateCommand()) {
                    // Query
                    String query = "SELECT COUNT(username) as exists FROM accounts WHERE username='" + username + "'";
                    dbcmd.CommandText = query;

                    using (OdbcDataReader dbreader = dbcmd.ExecuteReader()) {
                        while(dbreader.Read()){
                            if ((int)dbreader["exists"] == 1)
                                exists = true;
                        }

                        dbreader.Close();
                    }
                }

                odbc.Close();
            }
            catch (Exception e) {
                error = e.StackTrace;
            }

            // Exit
            return exists;
        }
    }
}