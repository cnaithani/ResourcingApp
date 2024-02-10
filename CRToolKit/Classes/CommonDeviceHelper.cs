using System;
using CRToolKit.Interfaces;

namespace CRToolKit
{
    public class CommonDeviceHelper : ICommonDeviceHelper
    {
        public string GetLocalFilePath(string filename)
        {
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            return Path.Combine(path, filename);
        }

        public async Task<string> GetDBFile()
        {
            string dbName = "AppSQLite.db3";
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Combine(path, dbName);
            // Check if your DB has already been extracted.
            if (!File.Exists(dbPath))
            {
                using (BinaryReader br = new BinaryReader(await FileSystem.OpenAppPackageFileAsync(dbName)))
                {
                    using (BinaryWriter bw = new BinaryWriter(new FileStream(dbPath, FileMode.Create)))
                    {
                        byte[] buffer = new byte[2048];
                        int len = 0;
                        while ((len = br.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            bw.Write(buffer, 0, len);
                        }
                    }
                }
            }
            

            return dbPath;
        }

        //This is a test function, will be used to copy user data file
        public string CopyDBFile()
        {
            string dbName = "AppSQLite.db3";
            string destName = "AppSQLite.txt";
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string dbPath = Path.Combine(path, dbName);

            string destPath = Path.Combine(Environment.SpecialFolder.LocalApplicationData.ToString(), destName);

            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, destPath, true);
            }

            return destPath;

        }

    }
}

