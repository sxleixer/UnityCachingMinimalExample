using System;
using ModuleInterface;

namespace UnityCachingMinimalExample
{
    public class Logger : ILogger
    {
        public void Log(string message) => Console.WriteLine($"[Playground] {message}");
        public void Log(Exception ex) => Console.WriteLine($"[Playground] Received {ex}");
    }
}