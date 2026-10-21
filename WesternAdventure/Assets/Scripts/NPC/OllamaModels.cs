using System;

namespace WesternAdventure.NPC
{
    [Serializable]
    internal class OllamaMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    internal class OllamaChatRequest
    {
        public string model;
        public OllamaMessage[] messages;
        public bool stream;
        public string format = "json";
    }

    [Serializable]
    internal class OllamaChatResponse
    {
        public OllamaMessage message;
        public bool done;
    }
}
