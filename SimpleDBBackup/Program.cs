using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using FluentFTP;
using Ionic.Zip;



namespace SimpleDBBackup
{
    class Program
    {
        static string connectionString;
        static string[] saDatabases;
        static string backupDir;
        static int nDeletionDays=0;
        static string UniqueId;
        static string ftpServer;
        static string ftpLogin;
        static string ftpPassword;
        static string ftpUploadDir;

        static void ReadSettings()
        {
            connectionString = ConfigurationManager.AppSettings["ConnectionString"];

            string strDatabases = ConfigurationManager.AppSettings["Databases"];
            if (!string.IsNullOrEmpty(strDatabases))
            {
                saDatabases = strDatabases.Split(',').Select(s => s.Trim()).ToArray();

                UniqueId = string.Join("_", saDatabases);
            }

            backupDir = ConfigurationManager.AppSettings["BackupDir"];

            string strDeletionDays = ConfigurationManager.AppSettings["DeletionDays"];
            int.TryParse(strDeletionDays, out nDeletionDays);

            ftpServer = ConfigurationManager.AppSettings["FtpServer"];
            ftpLogin = ConfigurationManager.AppSettings["FtpLogin"];
            ftpPassword = ConfigurationManager.AppSettings["FtpPassword"];
            ftpUploadDir = ConfigurationManager.AppSettings["FtpUploadDir"];
        }

        static void Main(string[] args)
        {
            ReadSettings();

            if (saDatabases == null)
            {
                Console.WriteLine("No databases to backup!");
                return;
            }

            Mutex mutex = new Mutex(true, $"Global\\SimpleDBBackupMutex_{UniqueId}");

            // allow only single instance of the app
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Console.WriteLine("Program already running!");
                return;
            }

            if (nDeletionDays > 0)
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
                            zip.UseZip64WhenSaving = Zip64Option.AsNecessary;
                            zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                            zip.AddFile(backupFileNameWithExt,"");
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

            if (!string.IsNullOrEmpty(ftpServer))
            {
                UploadToFTP();
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
                    if (fi.CreationTime < DateTime.Now.AddDays(-nDeletionDays))
                        fi.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void UploadToFTP()
        {
            using (FtpClient client = new FtpClient(ftpServer, ftpLogin, ftpPassword))
            {
                client.AutoConnect();

                client.UploadDirectory(backupDir, ftpUploadDir, FtpFolderSyncMode.Mirror);

                client.Disconnect();

            }
        }

        static Program()
        {

        }
    }
}
