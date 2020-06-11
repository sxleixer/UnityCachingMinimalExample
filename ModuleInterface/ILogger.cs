using System;

namespace ModuleInterface
{
    public interface ILogger
    {
        void Log(string message);
        void Log(Exception ex);
    }
}