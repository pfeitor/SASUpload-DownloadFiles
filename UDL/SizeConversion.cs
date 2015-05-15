using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class SizeConversion
    {
        const int KB = 1024;
        const int MB = 1048576;
        const int GB = 1073741824;
        public SizeConversion()
        {
        }

        public string size(long size)
        {
            if (size < KB)
            {
                return string.Format("{0} bytes", size);
            }
            else if (size < MB)
            {
                return string.Format("{0} KB", (size / KB));
            }
            else if (size < GB)
            {
               return string.Format("{0:0.00} MB", (size / MB));
            }
            else
            {
                return string.Format("{0:0.00} GB", (size / GB));
            }
        }
    }
}
