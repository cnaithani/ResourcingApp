using System;
namespace ResourcingToolKit.Interfaces
{
	public interface ICommonDeviceHelper
	{
        string GetLocalFilePath(string filename);

        Task<string> GetDBFile();

        string CopyDBFile();
    }
}

