using System;
using System.Collections.Generic;

namespace GameSaver.Util.Web.Response
{
    public class GetPaste
    {
        [Serializable]
        public class GetPasteResponse
        {
            public GetPasteData paste;
            public bool success;
        }

        [Serializable]
        public class GetPasteData
        {
            public string id;
            public bool encrypted;
            public string description;
            public int views;
            public DateTime created_at;
            public DateTime expires_at;
            public List<GetPasteSection> sections;
        }
    
        [Serializable]
        public class GetPasteSection
        {
            public int id;
            public string syntax;
            public string name;
            public string contents;
            public int size;
        }
    }
}
