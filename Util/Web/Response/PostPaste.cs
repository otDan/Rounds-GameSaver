using System;

namespace GameSaver.Util.Web.Response
{
    public class PostPaste
    {
        [Serializable]
        public class PostPasteResponse
        {
            public string id;
            public string link;
        }
    }
}
