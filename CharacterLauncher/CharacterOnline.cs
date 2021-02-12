using System;

namespace CharacterLauncher
{
    public class CharacterOnline
    {
        public long character_id { get; set; }
        public bool online { get; set; }
        public DateTime last_login { get; set; }
    }
}

