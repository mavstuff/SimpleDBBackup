using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;

namespace SimpleDBBackup
{
    class Program
    {
        /* SQL Server connecton string */
        static string connectionString = "Server=localhost;Integrated Security=True";

        //Optional:connect using credentials
        //static string connectionString = "Server=localhost;user id=user2018;password=MYDBPASSWORD;";

        /* Database names to backup */
        static string[] saDatabases = new string[] { "shop", "frontend", "accounting" };

        /* Backup directory. Please note: Files older than DeletionDays old will be deleted automatically */
        static string backupDir = "C:\\DB Backup";

        /* Delete backups older than DeletionDays. Set this to 0 to never delete backups */
        static int DeletionDays = 10;



        static Mutex mutex = new Mutex(true, "Global\\SimpleDBBackupMutex");
        static void Main(string[] args)
        {
            // allow only single instance of the app
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Console.WriteLine("Program already running!");
                return;
            }

            if (DeletionDays > 0)
                DeleteOldBackups();

            DateTime dtNow = DateTime.Now;

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();

                    foreach (string dbName in saDatabases)
                    {
                        string backupFileNameWithoutExt = String.Format("{0}\\{1}_{2:yyyy-MM-dd_hh-mm-ss-tt}", backupDir, dbName, dtNow);
                        string backupFileNameWithExt = String.Format("{0}.bak", backupFileNameWithoutExt);
                        string zipFileName = String.Format("{0}.zip", backupFileNameWithoutExt);

                        string cmdText = string.Format("BACKUP DATABASE {0}\r\nTO DISK = '{1}'", dbName, backupFileNameWithExt);

                        using (SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection))
                        {
                            sqlCommand.CommandTimeout = 0;
                            sqlCommand.ExecuteNonQuery();
                        }

                        using (ZipFile zip = new ZipFile())
                        {
                            zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                            zip.AddFile(backupFileNameWithExt);
                            zip.Save(zipFileName);
                        }
                        
                        File.Delete(backupFileNameWithExt);
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            mutex.ReleaseMutex();
        }

        static void DeleteOldBackups()
        {
            try
            {
                string[] files = Directory.GetFiles(backupDir);

                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.CreationTime < DateTime.Now.AddDays(-DeletionDays))
                        fi.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
