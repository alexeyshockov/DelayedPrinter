using System.Collections.Generic;

namespace DelayedPrinter.Tests
{
    internal class TerminalStub : ITerminal
    {
        public List<string> PrintedMessages { get; } = new();

        public void WriteLine(string text) => PrintedMessages.Add(text);
    }
}