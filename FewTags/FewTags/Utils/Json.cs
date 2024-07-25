using System;
using System.Collections.Generic;

namespace FewTags.Utils
{
    internal class Json
    {
        [Serializable]
        public class _Tags
        {
            public List<User> records { get; set; }
            //public string UserID { get; set; }
            //public Tags UserTags { get; set; }
        }
        public class User
        {
            public int id { get; set; }
            public string UserId { get; set; }
            public string[] NamePlatesText { get; set; }
            public string[] BigPlatesText { get; set; }
            public int[] Color { get; set; }
        }
    }
}
