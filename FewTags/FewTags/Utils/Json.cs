using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FewTags.Utils
{
    internal class Json
    {
        [Serializable]
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
