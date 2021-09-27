using System;

namespace DelayedPrinter
{
    public interface ITerminal
    {
        void WriteLine(string text);
    }

    public class Terminal : ITerminal
    {
        public void WriteLine(string text) => Console.WriteLine(text);
    }
}
