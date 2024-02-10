using System;
namespace CRToolKit.Interfaces
{
	public interface ICommonDeviceHelper
	{
        string GetLocalFilePath(string filename);

        Task<string> GetDBFile();

        string CopyDBFile();
    }
}

