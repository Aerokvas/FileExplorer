using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMenager
{
    public class FileItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string FullPath => Path.Combine(Directory, Name);
        public string Directory { get; set; } // новое поле
    }

}
