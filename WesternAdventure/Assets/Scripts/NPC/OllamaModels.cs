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
    internal class OllamaOptions
    {
        // Düşük sıcaklık, modelin diyalog şemasına ve Türkçe kısıtına daha tutarlı uymasını sağlar.
        public float temperature = 0.2f;
    }

    [Serializable]
    internal class OllamaChatRequest
    {
        public string model;
        public OllamaMessage[] messages;
        public bool stream;
        public string format = "json";
        public OllamaOptions options = new OllamaOptions();
    }

    [Serializable]
    internal class OllamaChatResponse
    {
        public OllamaMessage message;
        public bool done;
    }
}
