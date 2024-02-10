using System;
using SQLite;

namespace CRToolKit.Data
{
	public class AppDatabase
	{
        public SQLiteAsyncConnection database;
        private DatabaseUpdates updates;
        public AppDatabase(string dbPath)
        {
            initializeDB(dbPath);
        }
        public async void initializeDB(string dbPath)
        {
            database = new SQLiteAsyncConnection(dbPath);
            await database.EnableWriteAheadLoggingAsync();
        }
        public async Task UpdateDatabase()
        {
            updates = new DatabaseUpdates();
            await updates.UpdateDatabase();
        }

    }
}

