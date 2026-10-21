using System;

namespace WesternAdventure.NPC
{
    /// <summary>Ollama sunucusuna ulaşılamadığında veya istek zaman aşımına uğradığında fırlatılır.</summary>
    public class OllamaConnectionException : Exception
    {
        public OllamaConnectionException(string message) : base(message) { }
    }

    /// <summary>Model geçerli/parse edilebilir bir JSON yanıtı üretemediğinde fırlatılır.</summary>
    public class OllamaParseException : Exception
    {
        public OllamaParseException(string message) : base(message) { }
    }
}
